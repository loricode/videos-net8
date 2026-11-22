using System.Net;
using System.Net.Http.Json;
using DomainCore.DTOs.Course;
using DomainCore.DTOs.Progress;
using DomainCore.DTOs.Video;

namespace WebApiCore.Tests;

public class FeatureTests : IClassFixture<ApiFactory>
{
    private readonly ApiFactory _api;

    public FeatureTests(ApiFactory api) => _api = api;

    [Fact]
    public async Task Catalogo_publico_solo_muestra_cursos_activos()
    {
        var (instructor, _) = await _api.InstructorAsync();
        var tag = Guid.NewGuid().ToString("N")[..8];
        await _api.CreateCourseAsync(instructor, $"Publicado {tag}", state: true);
        await _api.CreateCourseAsync(instructor, $"Borrador {tag}", state: false);

        var result = await _api.CreateClient().GetFromJsonAsync<List<CourseDetailDto>>($"/Course/Search?parameter={tag}");

        Assert.Single(result!);
        Assert.StartsWith("Publicado", result![0].Title);
        Assert.StartsWith("/images/", result[0].ImgUrl);
    }

    [Fact]
    public async Task Lecciones_publicas_solo_incluyen_videos_listos()
    {
        var (instructor, _) = await _api.InstructorAsync();
        var courseId = await _api.CreateCourseAsync(instructor);

        var v1 = await (await instructor.PostAsync("/Video/Upload", ApiFactory.VideoForm(courseId))).Content.ReadFromJsonAsync<Guid>();
        await instructor.PostAsync("/Video/Upload", ApiFactory.VideoForm(courseId));
        await _api.MarkReadyAsync(v1);

        var pub = await _api.CreateClient().GetFromJsonAsync<VideoDetailDto>($"/Video/ListByCourseId?parameter={courseId}");
        Assert.Single(pub!.Items);
        Assert.Equal(v1, pub.Items[0].Id);

        var admin = await instructor.GetFromJsonAsync<List<VideoDto>>($"/Video/List/{courseId}");
        Assert.Equal(2, admin!.Count);
        Assert.All(admin, v => Assert.True(v.CreatedAt > DateTime.UtcNow.AddMinutes(-5)));
    }

    [Fact]
    public async Task Eliminar_leccion_borra_sus_archivos()
    {
        var (instructor, _) = await _api.InstructorAsync();
        var courseId = await _api.CreateCourseAsync(instructor);
        var videoId = await (await instructor.PostAsync("/Video/Upload", ApiFactory.VideoForm(courseId))).Content.ReadFromJsonAsync<Guid>();
        var folder = Path.Combine(_api.MediaRoot, "storage", courseId.ToString(), videoId.ToString());
        Assert.True(Directory.Exists(folder));

        var del = await instructor.DeleteAsync($"/Video/Delete?courseId={courseId}&videoId={videoId}");
        del.EnsureSuccessStatusCode();

        Assert.False(Directory.Exists(folder));
    }

    [Fact]
    public async Task Eliminar_curso_borra_portada_y_videos()
    {
        var (instructor, _) = await _api.InstructorAsync();
        var courseId = await _api.CreateCourseAsync(instructor);
        await instructor.PostAsync("/Video/Upload", ApiFactory.VideoForm(courseId));

        (await instructor.DeleteAsync($"/Course/Delete?courseId={courseId}")).EnsureSuccessStatusCode();

        Assert.False(Directory.Exists(Path.Combine(_api.MediaRoot, "storage", courseId.ToString())));
        Assert.False(Directory.Exists(Path.Combine(_api.MediaRoot, "images", courseId.ToString())));
    }

    [Fact]
    public async Task Cambiar_portada_reemplaza_la_anterior()
    {
        var (instructor, _) = await _api.InstructorAsync();
        var courseId = await _api.CreateCourseAsync(instructor);
        await Task.Delay(1100); // nombre con marca de tiempo por segundo

        var form = ApiFactory.CourseForm("x");
        form.Add(new StringContent(courseId.ToString()), "courseId");
        (await instructor.PutAsync("/Course/UpdateImage", form)).EnsureSuccessStatusCode();

        var files = Directory.GetFiles(Path.Combine(_api.MediaRoot, "images", courseId.ToString()));
        Assert.Single(files);
    }

    [Fact]
    public async Task Reintentar_solo_aplica_a_lecciones_con_error()
    {
        var (instructor, _) = await _api.InstructorAsync();
        var courseId = await _api.CreateCourseAsync(instructor);
        var videoId = await (await instructor.PostAsync("/Video/Upload", ApiFactory.VideoForm(courseId))).Content.ReadFromJsonAsync<Guid>();

        var res = await instructor.PostAsync($"/Video/Reprocess?videoId={videoId}", null);
        Assert.Equal(HttpStatusCode.Conflict, res.StatusCode);
    }

    [Fact]
    public async Task Estadisticas_resumen_del_instructor()
    {
        var (instructor, _) = await _api.InstructorAsync();
        var c1 = await _api.CreateCourseAsync(instructor, "Activo", true);
        await _api.CreateCourseAsync(instructor, "Inactivo", false);
        var v1 = await (await instructor.PostAsync("/Video/Upload", ApiFactory.VideoForm(c1))).Content.ReadFromJsonAsync<Guid>();
        await instructor.PostAsync("/Video/Upload", ApiFactory.VideoForm(c1));
        await _api.MarkReadyAsync(v1);

        var stats = await instructor.GetFromJsonAsync<CourseStatsDto>("/Course/Stats");

        Assert.Equal(2, stats!.Courses);
        Assert.Equal(1, stats.Active);
        Assert.Equal(1, stats.Inactive);
        Assert.Equal(2, stats.TotalVideos);
        Assert.Equal(1, stats.Ready);
        Assert.Equal(1, stats.Processing);
        Assert.Equal(7, stats.Last7Days.Count);
        Assert.Equal(2, stats.Last7Days[^1].Count);
        Assert.Equal("Activo", stats.RecentCourses[0].Title);
        Assert.Equal(2, stats.RecentCourses[0].VideoCount);
    }

    [Fact]
    public async Task Progreso_del_estudiante_se_guarda_en_el_servidor()
    {
        var (instructor, _) = await _api.InstructorAsync();
        var courseId = await _api.CreateCourseAsync(instructor);
        var ready = await (await instructor.PostAsync("/Video/Upload", ApiFactory.VideoForm(courseId))).Content.ReadFromJsonAsync<Guid>();
        var pending = await (await instructor.PostAsync("/Video/Upload", ApiFactory.VideoForm(courseId))).Content.ReadFromJsonAsync<Guid>();
        await _api.MarkReadyAsync(ready);

        var (student, _) = await _api.StudentAsync();

        (await student.PutAsJsonAsync("/Progress", new { videoId = ready, positionSeconds = 42.5, completed = false })).EnsureSuccessStatusCode();
        (await student.PutAsJsonAsync("/Progress", new { videoId = ready, positionSeconds = 90, completed = true })).EnsureSuccessStatusCode();
        // completada no retrocede
        (await student.PutAsJsonAsync("/Progress", new { videoId = ready, positionSeconds = 10, completed = false })).EnsureSuccessStatusCode();

        var list = await student.GetFromJsonAsync<List<ProgressDto>>($"/Progress/Course/{courseId}");
        var p = Assert.Single(list!);
        Assert.True(p.Completed);
        Assert.Equal(10, p.PositionSeconds);

        var notReady = await student.PutAsJsonAsync("/Progress", new { videoId = pending, positionSeconds = 1, completed = false });
        Assert.Equal(HttpStatusCode.NotFound, notReady.StatusCode);
    }
}

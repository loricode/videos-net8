using System.Net;
using System.Net.Http.Json;
using DomainCore.DTOs.Course;
using DomainCore.DTOs.User;

namespace WebApiCore.Tests;

public class SecurityTests : IClassFixture<ApiFactory>
{
    private readonly ApiFactory _api;

    public SecurityTests(ApiFactory api) => _api = api;

    [Theory]
    [InlineData("GET", "/Course/List")]
    [InlineData("GET", "/Course/Stats")]
    [InlineData("POST", "/Video/Upload")]
    [InlineData("DELETE", "/Course/Delete?courseId=00000000-0000-0000-0000-000000000001")]
    [InlineData("GET", "/Progress/Course/00000000-0000-0000-0000-000000000001")]
    public async Task Endpoints_protegidos_rechazan_sin_token(string method, string url)
    {
        var res = await _api.CreateClient().SendAsync(new HttpRequestMessage(new HttpMethod(method), url));
        Assert.Equal(HttpStatusCode.Unauthorized, res.StatusCode);
    }

    [Fact]
    public async Task Estudiante_no_puede_gestionar_cursos()
    {
        var (student, _) = await _api.StudentAsync();
        Assert.Equal(HttpStatusCode.Forbidden, (await student.GetAsync("/Course/List")).StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden, (await student.PostAsync("/Course/Create", ApiFactory.CourseForm("x"))).StatusCode);
    }

    [Fact]
    public async Task Instructor_no_puede_operar_sobre_cursos_ajenos_aunque_envie_el_userId_del_duenio()
    {
        var (owner, ownerId) = await _api.InstructorAsync();
        var (intruder, _) = await _api.InstructorAsync();
        var courseId = await _api.CreateCourseAsync(owner, "Curso privado");

        // Obtener
        Assert.Equal(HttpStatusCode.NotFound,
            (await intruder.GetAsync($"/Course/Get?courseId={courseId}&userId={ownerId}")).StatusCode);

        // Editar (userId del dueño en el formulario: debe ignorarse)
        var update = new MultipartFormDataContent
        {
            { new StringContent(courseId.ToString()), "courseId" },
            { new StringContent("Hackeado"), "title" },
            { new StringContent("false"), "state" },
            { new StringContent(ownerId.ToString()), "userId" },
        };
        Assert.Equal(HttpStatusCode.NotFound, (await intruder.PutAsync("/Course/Update", update)).StatusCode);

        // Eliminar
        Assert.Equal(HttpStatusCode.NotFound,
            (await intruder.DeleteAsync($"/Course/Delete?courseId={courseId}&userId={ownerId}")).StatusCode);

        // Subir video
        Assert.Equal(HttpStatusCode.NotFound,
            (await intruder.PostAsync("/Video/Upload", ApiFactory.VideoForm(courseId))).StatusCode);

        // Listar videos
        Assert.Equal(HttpStatusCode.NotFound, (await intruder.GetAsync($"/Video/List/{courseId}")).StatusCode);

        // La variante antigua List/{userId} no filtra datos de otro usuario
        var leaked = await intruder.GetFromJsonAsync<List<CourseDto>>($"/Course/List/{ownerId}");
        Assert.Empty(leaked!);

        // El dueño conserva su curso intacto
        var mine = await owner.GetFromJsonAsync<CourseDto>($"/Course/Get?courseId={courseId}");
        Assert.Equal("Curso privado", mine!.Title);
    }

    [Fact]
    public async Task Login_invalido_devuelve_mensaje_generico()
    {
        var client = _api.CreateClient();
        var res = await client.PostAsJsonAsync("/User/Login", new { email = "nadie@test.local", password = "Clave123!" });
        Assert.Equal(HttpStatusCode.NotFound, res.StatusCode);
        var body = await res.Content.ReadFromJsonAsync<System.Text.Json.JsonElement>();
        Assert.Equal("Correo o contraseña incorrectos.", body.GetProperty("message").GetString());
    }

    [Fact]
    public async Task Registro_publico_crea_estudiante_y_valida_contrasenia()
    {
        var client = _api.CreateClient();
        var email = $"alumno-{Guid.NewGuid():N}@test.local";

        var weak = await client.PostAsJsonAsync("/User/Register", new { fullName = "Ana", email, password = "debil" });
        Assert.Equal(HttpStatusCode.BadRequest, weak.StatusCode);

        var ok = await client.PostAsJsonAsync("/User/Register", new { fullName = "Ana", email, password = "Clave123!" });
        ok.EnsureSuccessStatusCode();

        var login = await client.PostAsJsonAsync("/User/Login", new { email, password = "Clave123!" });
        var dto = await login.Content.ReadFromJsonAsync<UserDetailDto>();
        Assert.Equal(["Student"], dto!.Roles);
        Assert.Equal("Ana", dto.FullName);
    }

    [Fact]
    public async Task CreateUser_ya_no_es_anonimo()
    {
        var res = await _api.CreateClient().PostAsJsonAsync("/User/CreateUser",
            new { fullName = "X", email = "x@test.local", password = "Clave123!" });
        Assert.Equal(HttpStatusCode.Unauthorized, res.StatusCode);
    }

    [Fact]
    public async Task Rechaza_archivos_con_extension_no_permitida()
    {
        var (instructor, _) = await _api.InstructorAsync();
        var bad = await instructor.PostAsync("/Course/Create", ApiFactory.CourseForm("x", fileName: "malware.exe"));
        Assert.Equal(HttpStatusCode.BadRequest, bad.StatusCode);

        var courseId = await _api.CreateCourseAsync(instructor);
        var badVideo = await instructor.PostAsync("/Video/Upload", ApiFactory.VideoForm(courseId, "script.sh"));
        Assert.Equal(HttpStatusCode.BadRequest, badVideo.StatusCode);
    }

    [Fact]
    public async Task Nombre_de_archivo_con_path_traversal_no_escapa_de_la_carpeta()
    {
        var (instructor, _) = await _api.InstructorAsync();
        var courseId = await _api.CreateCourseAsync(instructor);
        var res = await instructor.PostAsync("/Video/Upload", ApiFactory.VideoForm(courseId, "../../../../evil.mp4"));
        res.EnsureSuccessStatusCode();

        Assert.False(File.Exists(Path.Combine(_api.MediaRoot, "..", "evil.mp4")));
        var videoId = await res.Content.ReadFromJsonAsync<Guid>();
        Assert.True(File.Exists(Path.Combine(_api.MediaRoot, "storage", courseId.ToString(), videoId.ToString(), "source.mp4")));
    }

    [Fact]
    public async Task El_archivo_original_nunca_se_sirve_publicamente()
    {
        var (instructor, _) = await _api.InstructorAsync();
        var courseId = await _api.CreateCourseAsync(instructor);
        var res = await instructor.PostAsync("/Video/Upload", ApiFactory.VideoForm(courseId));
        var videoId = await res.Content.ReadFromJsonAsync<Guid>();

        var download = await _api.CreateClient().GetAsync($"/storage/{courseId}/{videoId}/source.mp4");
        Assert.Equal(HttpStatusCode.NotFound, download.StatusCode);
    }
}

public class RateLimitTests
{
    [Fact]
    public async Task Login_se_limita_por_IP()
    {
        await using var api = new ApiFactory { AuthPermitPerMinute = 3 };
        var client = api.CreateClient();
        var codes = new List<HttpStatusCode>();
        for (var i = 0; i < 5; i++)
        {
            var r = await client.PostAsJsonAsync("/User/Login", new { email = "a@test.local", password = "Clave123!" });
            codes.Add(r.StatusCode);
        }
        Assert.Equal(HttpStatusCode.TooManyRequests, codes[^1]);
        Assert.Equal(3, codes.Count(c => c != HttpStatusCode.TooManyRequests));
    }
}

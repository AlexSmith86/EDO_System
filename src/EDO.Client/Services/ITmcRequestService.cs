using System.Net.Http;
using System.Net.Http.Headers;
using System.Net.Http.Json;

namespace EDO.Client.Services;

public class TmcRequestDto
{
    public int Id { get; set; }
    public int InitiatorUserId { get; set; }
    public string InitiatorName { get; set; } = string.Empty;
    public string? ProjectName { get; set; }
    public string Status { get; set; } = string.Empty;
    public string StatusDisplay { get; set; } = string.Empty;
    public int? CurrentStageId { get; set; }
    public string? CurrentStageName { get; set; }
    public string? CurrentStagePosition { get; set; }
    public string? CurrentStageDescription { get; set; }
    public int CurrentStageOrder { get; set; }
    public int TotalStages { get; set; }
    public int? WorkflowChainId { get; set; }
    public string? WorkflowChainName { get; set; }
    public int? ResponsibleUserId { get; set; }
    public string? ResponsibleUserName { get; set; }
    public string? ResponsibleUserPosition { get; set; }
    public DateTime CreatedAt { get; set; }
    public List<TmcRequestItemDto> Items { get; set; } = new();
    public List<ApprovalHistoryDto> ApprovalHistory { get; set; } = new();
}

public class AssignResponsibleDto
{
    public int? TargetUserId { get; set; }
}

public class ApprovalHistoryDto
{
    public int Id { get; set; }
    public string StageName { get; set; } = string.Empty;
    public string StagePosition { get; set; } = string.Empty;
    public string Decision { get; set; } = string.Empty;
    public string DecisionDisplay { get; set; } = string.Empty;
    public string UserName { get; set; } = string.Empty;
    public string? UserPosition { get; set; }
    public string? Comment { get; set; }
    public DateTime CreatedAt { get; set; }

    /// <summary>Оригинальное имя прикреплённого к решению файла (для отображения).</summary>
    public string? AttachedFileName { get; set; }

    /// <summary>Относительный URL вложения — «/uploads/attachments/{guid}.ext».
    /// Клиент открывает его по базовому адресу HttpClient.</summary>
    public string? AttachedFileUrl { get; set; }
}

/// <summary>Ответ эндпоинта загрузки вложения.</summary>
public class UploadedAttachmentDto
{
    public string FileName { get; set; } = string.Empty;
    public string Url { get; set; } = string.Empty;
    public long Size { get; set; }
}

public class TmcRequestItemDto
{
    public int Id { get; set; }
    public int? GroupId { get; set; }
    public string? GroupName { get; set; }
    public int? SubgroupId { get; set; }
    public string? SubgroupName { get; set; }
    public string Name { get; set; } = string.Empty;
    public decimal Quantity { get; set; }
    public string? Unit { get; set; }
    public decimal? Price { get; set; }
    public DateTime? PlannedDeliveryDate { get; set; }
    public string? InvoiceLink { get; set; }
    public string? Comment { get; set; }
    public string? InitiatorName { get; set; }
    public string? InitiatorPosition { get; set; }
}

public class CreateTmcRequestDto
{
    public string? ProjectName { get; set; }
    public int? WorkflowChainId { get; set; }
    public List<CreateTmcRequestItemDto> Items { get; set; } = new();
}

public class CreateTmcRequestItemDto
{
    public int? GroupId { get; set; }
    public int? SubgroupId { get; set; }
    public string Name { get; set; } = string.Empty;
    public decimal Quantity { get; set; }
    public string? Unit { get; set; }
    public decimal? Price { get; set; }
    public DateTime? PlannedDeliveryDate { get; set; }
    public string? InvoiceLink { get; set; }
    public string? Comment { get; set; }
    public string? InitiatorName { get; set; }
    public string? InitiatorPosition { get; set; }
}

public class UpdateTmcRequestDto
{
    public string? ProjectName { get; set; }
    public List<CreateTmcRequestItemDto> Items { get; set; } = new();
}

public class SubmitDecisionDto
{
    public string Decision { get; set; } = string.Empty;
    public string? Comment { get; set; }

    /// <summary>Для отклонения стандартного маршрута — Id целевого ApprovalStage.
    /// Null = возврат к инициатору (OrderSequence = 0).</summary>
    public int? TargetStageId { get; set; }

    /// <summary>Для отклонения кастомной цепочки — Id целевого WorkflowStep.
    /// Null = возврат к инициатору.</summary>
    public int? TargetWorkflowStepId { get; set; }

    /// <summary>Оригинальное имя прикреплённого файла (опционально).
    /// Предполагается, что файл уже загружен через UploadAttachmentAsync.</summary>
    public string? AttachedFileName { get; set; }

    /// <summary>Относительный URL прикреплённого файла, возвращённый сервером при загрузке.</summary>
    public string? AttachedFileUrl { get; set; }
}

public class ReturnTargetDto
{
    public int? StageId { get; set; }
    public int? WorkflowStepId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Position { get; set; } = string.Empty;
    public int Order { get; set; }
    public bool IsInitiator { get; set; }
}

public class SendRequestDto
{
    /// <summary>Id целевого ApprovalStage для стандартного маршрута.
    /// Null — стартуем с первого (OrderSequence = 1).</summary>
    public int? TargetStageId { get; set; }

    /// <summary>Id целевого WorkflowStep для кастомной цепочки.
    /// Null — стартуем с первого шага цепочки.</summary>
    public int? TargetWorkflowStepId { get; set; }
}

public class ForwardTargetDto
{
    public int? StageId { get; set; }
    public int? WorkflowStepId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Position { get; set; } = string.Empty;
    public string? Description { get; set; }
    public int Order { get; set; }
    public bool IsDefault { get; set; }
}

public interface ITmcRequestService
{
    Task<List<TmcRequestDto>> GetMyAsync();
    Task<List<TmcRequestDto>> GetPendingAsync();
    Task<List<TmcRequestDto>> GetApprovedAsync();
    Task<TmcRequestDto?> GetByIdAsync(int id);
    Task<TmcRequestDto?> CreateAsync(CreateTmcRequestDto dto);
    Task<TmcRequestDto?> UpdateAsync(int id, UpdateTmcRequestDto dto);
    Task<TmcRequestDto?> SendAsync(int id, SendRequestDto? dto = null);
    Task<List<ForwardTargetDto>> GetForwardTargetsAsync(int id);
    Task<TmcRequestDto?> SubmitDecisionAsync(int id, SubmitDecisionDto dto);
    Task<List<ReturnTargetDto>> GetReturnTargetsAsync(int id);
    Task<TmcRequestDto?> AssignResponsibleAsync(int id, AssignResponsibleDto dto);
    Task DeleteAsync(int id);
    Task<List<string>> GetProjectsAsync();

    /// <summary>Загрузить файл-вложение для заявки (multipart).
    /// Возвращает метаданные, которые затем кладутся в SubmitDecisionDto.</summary>
    Task<UploadedAttachmentDto?> UploadAttachmentAsync(int requestId, Stream content, string fileName, string contentType);

    /// <summary>Построить абсолютный URL файла по относительному пути
    /// («/uploads/attachments/xxx.pdf»), учитывая BaseAddress HttpClient'а.</summary>
    string BuildAttachmentAbsoluteUrl(string relativeUrl);
}

public class TmcRequestService : ITmcRequestService
{
    private readonly HttpClient _http;

    public TmcRequestService(HttpClient http)
    {
        _http = http;
    }

    public async Task<List<TmcRequestDto>> GetMyAsync()
    {
        return await _http.GetFromJsonAsync<List<TmcRequestDto>>("api/tmcrequests/my") ?? new();
    }

    public async Task<TmcRequestDto?> GetByIdAsync(int id)
    {
        return await _http.GetFromJsonAsync<TmcRequestDto>($"api/tmcrequests/{id}");
    }

    public async Task<TmcRequestDto?> CreateAsync(CreateTmcRequestDto dto)
    {
        var response = await _http.PostAsJsonAsync("api/tmcrequests", dto);
        response.EnsureSuccessStatusCode();
        return await response.Content.ReadFromJsonAsync<TmcRequestDto>();
    }

    public async Task<TmcRequestDto?> UpdateAsync(int id, UpdateTmcRequestDto dto)
    {
        var response = await _http.PutAsJsonAsync($"api/tmcrequests/{id}", dto);
        response.EnsureSuccessStatusCode();
        return await response.Content.ReadFromJsonAsync<TmcRequestDto>();
    }

    public async Task<TmcRequestDto?> SendAsync(int id, SendRequestDto? dto = null)
    {
        // Всегда отправляем JSON-тело — бэкенд принимает DTO как необязательное тело
        var response = await _http.PostAsJsonAsync($"api/tmcrequests/{id}/send", dto ?? new SendRequestDto());
        if (!response.IsSuccessStatusCode)
        {
            var body = await response.Content.ReadAsStringAsync();
            throw new HttpRequestException($"{(int)response.StatusCode}: {body}");
        }
        return await response.Content.ReadFromJsonAsync<TmcRequestDto>();
    }

    public async Task<List<ForwardTargetDto>> GetForwardTargetsAsync(int id)
    {
        return await _http.GetFromJsonAsync<List<ForwardTargetDto>>($"api/tmcrequests/{id}/forward-targets")
               ?? new List<ForwardTargetDto>();
    }

    public async Task<List<TmcRequestDto>> GetPendingAsync()
    {
        return await _http.GetFromJsonAsync<List<TmcRequestDto>>("api/tmcrequests/pending") ?? new();
    }

    public async Task<List<TmcRequestDto>> GetApprovedAsync()
    {
        return await _http.GetFromJsonAsync<List<TmcRequestDto>>("api/tmcrequests/approved") ?? new();
    }

    public async Task<TmcRequestDto?> SubmitDecisionAsync(int id, SubmitDecisionDto dto)
    {
        var response = await _http.PostAsJsonAsync($"api/tmcrequests/{id}/decision", dto);
        if (!response.IsSuccessStatusCode)
        {
            var body = await response.Content.ReadAsStringAsync();
            throw new HttpRequestException($"{(int)response.StatusCode}: {body}");
        }
        return await response.Content.ReadFromJsonAsync<TmcRequestDto>();
    }

    public async Task<List<ReturnTargetDto>> GetReturnTargetsAsync(int id)
    {
        return await _http.GetFromJsonAsync<List<ReturnTargetDto>>($"api/tmcrequests/{id}/return-targets")
               ?? new List<ReturnTargetDto>();
    }

    public async Task<TmcRequestDto?> AssignResponsibleAsync(int id, AssignResponsibleDto dto)
    {
        var response = await _http.PostAsJsonAsync($"api/tmcrequests/{id}/assign", dto);
        if (!response.IsSuccessStatusCode)
        {
            var body = await response.Content.ReadAsStringAsync();
            throw new HttpRequestException($"{(int)response.StatusCode}: {body}");
        }
        return await response.Content.ReadFromJsonAsync<TmcRequestDto>();
    }

    public async Task DeleteAsync(int id)
    {
        var response = await _http.DeleteAsync($"api/tmcrequests/{id}");
        response.EnsureSuccessStatusCode();
    }

    public async Task<List<string>> GetProjectsAsync()
    {
        return await _http.GetFromJsonAsync<List<string>>("api/tmcrequests/projects") ?? new();
    }

    public async Task<UploadedAttachmentDto?> UploadAttachmentAsync(
        int requestId, Stream content, string fileName, string contentType)
    {
        // Лимит 50 МБ (должен совпадать с Kestrel/LocalFileStorageService на сервере).
        const long maxBytes = 50L * 1024 * 1024;

        using var form = new MultipartFormDataContent();
        var streamContent = new StreamContent(content, bufferSize: 81920);
        streamContent.Headers.ContentType = string.IsNullOrWhiteSpace(contentType)
            ? new MediaTypeHeaderValue("application/octet-stream")
            : new MediaTypeHeaderValue(contentType);
        form.Add(streamContent, "file", fileName);

        // Для Blazor WASM важно явно отключить буферизацию ответа, чтобы
        // большие запросы уезжали потоком, а не копились целиком в памяти.
        var request = new HttpRequestMessage(HttpMethod.Post, $"api/tmcrequests/{requestId}/attachment")
        {
            Content = form
        };

        var response = await _http.SendAsync(request, HttpCompletionOption.ResponseHeadersRead);
        if (!response.IsSuccessStatusCode)
        {
            var body = await response.Content.ReadAsStringAsync();
            if (response.StatusCode == System.Net.HttpStatusCode.RequestEntityTooLarge
                || (!string.IsNullOrEmpty(body) && body.Contains("слишком большой", StringComparison.OrdinalIgnoreCase)))
            {
                throw new HttpRequestException(
                    $"Файл превышает лимит {maxBytes / (1024 * 1024)} МБ.");
            }
            throw new HttpRequestException($"{(int)response.StatusCode}: {body}");
        }

        return await response.Content.ReadFromJsonAsync<UploadedAttachmentDto>();
    }

    public string BuildAttachmentAbsoluteUrl(string relativeUrl)
    {
        if (string.IsNullOrWhiteSpace(relativeUrl))
            return string.Empty;

        // Если уже абсолютный URL — отдаём как есть.
        if (Uri.TryCreate(relativeUrl, UriKind.Absolute, out _))
            return relativeUrl;

        var baseAddress = _http.BaseAddress;
        if (baseAddress is null)
            return relativeUrl;

        // Uri корректно склеивает host + «/uploads/...» вне зависимости от того,
        // есть ли в BaseAddress финальный слэш.
        return new Uri(baseAddress, relativeUrl).ToString();
    }
}

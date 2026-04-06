namespace EDO.Server.DTOs;

public class TmcGroupDto
{
    public int Id { get; set; }
    public string Code { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string DisplayName => $"{Code}. {Name}";
}

public class TmcSubgroupDto
{
    public int Id { get; set; }
    public string Code { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public int GroupId { get; set; }
    public bool IsHeader { get; set; }
    public string DisplayName => IsHeader ? Name : $"{Code}. {Name}";
}

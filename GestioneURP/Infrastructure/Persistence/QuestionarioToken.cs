namespace GestioneURP.Infrastructure.Persistence;

public class QuestionarioToken
{
	public string Token { get; set; } = string.Empty;
	public string? TipoQuestionarioCodice { get; set; }
	public string? Sorgente { get; set; }
	public DateTime? DataUtilizzo { get; set; }
	public string? Email { get; set; }
}

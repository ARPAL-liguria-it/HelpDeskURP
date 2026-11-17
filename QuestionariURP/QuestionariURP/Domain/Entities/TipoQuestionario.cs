/*
 * Nome del progetto: HelpDesk URP
 * Copyright (C) 2026 Agenzia regionale per la protezione dell'ambiente ligure
 * ---
 * Questo programma è software libero: puoi ridistribuirlo e/o modificarlo
 * secondo i termini della GNU Affero General Public License pubblicata dalla
 * Free Software Foundation, sia la versione 3 della licenza, sia (a tua scelta)
 * qualsiasi versione successiva.
 * ---
 * Questo programma è distribuito nella speranza che possa essere utile,
 * ma SENZA ALCUNA GARANZIA; senza nemmeno la garanzia implicita di
 * COMMERCIABILITÀ o IDONEITÀ PER UNO SCOPO PARTICOLARE. Vedi la
 * GNU Affero General Public License per ulteriori dettagli.
 * ---
 * Dovresti aver ricevuto una copia della GNU Affero General Public License
 * insieme a questo programma. In caso contrario, vedi <https://www.gnu.org/licenses/>.
*/

namespace QuestionariURP.Domain.Entities;

/// <summary>
/// Tipo di questionario configurato da un sistema esterno.
/// Entità in sola lettura per questa applicazione.
/// Tabella fisica: URP_HD.QUESTIONARI_TIPO
/// </summary>
public class TipoQuestionario
{
	public string Codice { get; set; } = string.Empty;
	public string Nome { get; set; } = string.Empty;
	public string? Descrizione { get; set; }

	/// <summary>Domande ordinate per Ordine.</summary>
	public List<Domanda> Domande { get; set; } = new();
}

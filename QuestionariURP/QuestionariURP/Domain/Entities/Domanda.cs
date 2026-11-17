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
/// Domanda di un questionario. Classe base per DomandaChiusa e DomandaAperta.
/// Il tipo concreto è discriminato dalla colonna TIPO_DOMANDA ('C' / 'A').
/// Entità in sola lettura per questa applicazione.
/// Tabella fisica: URP_HD.QUESTIONARI_DOMANDA
/// </summary>
public abstract class Domanda
{
	public long Id { get; set; }
	public string TipoQuestionarioCodice { get; set; } = string.Empty;
	public int Ordine { get; set; }
	public string Testo { get; set; } = string.Empty;
}

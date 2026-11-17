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

namespace GestioneURP.Infrastructure.Persistence;

public class RegistroAttivita
{
	public long IdLog { get; set; }
	public string TipoSegnalazione { get; set; } = string.Empty;
	public long IdSegnalazione { get; set; }
	public DateTime DataOraEvento { get; set; }
	public string Operatore { get; set; } = string.Empty;
	public string StatoPratica { get; set; } = string.Empty;
	public string? Dettagli { get; set; }
}

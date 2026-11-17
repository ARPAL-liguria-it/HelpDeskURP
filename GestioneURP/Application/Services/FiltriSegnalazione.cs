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

namespace GestioneURP.Application.Services;

/// <summary>
/// Raccoglie i criteri della lista segnalazioni: valori nulli o testo vuoto non applicano il relativo filtro.
/// La tematica riguarda soltanto le richieste, quindi la sua selezione esclude i reclami dalla lista.
/// </summary>
public class FiltriSegnalazione
{
	public string? Testo { get; set; }
	public long? IdTipoTematica { get; set; }
	public string? StatoPratica { get; set; }
}

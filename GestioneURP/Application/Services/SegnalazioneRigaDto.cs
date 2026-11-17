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
/// Rappresenta una riga della lista unificata di richieste e reclami, con descrizioni già risolte dal servizio.
/// TipoSegnalazione e Id identificano insieme il dettaglio da aprire.
/// </summary>
public record SegnalazioneRigaDto(
	string TipoSegnalazione,
	long Id,
	string? Codice,
	DateTime? DataInserimento,
	string? EmailAutore,
	string? CognomeNomeAutore,
	string? Tipologia,
	string? AreaTematica,
	string? StatoPratica,
	string? OperatorePresaInCarico
);

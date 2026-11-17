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

using NLog;
using NLog.Web;
using Microsoft.Extensions.Configuration;
using System.IO;
using System;
using System.Threading.Tasks;

public class PuliziaFileTemporaneiService
{
	private readonly IConfiguration _configuration;

	public PuliziaFileTemporaneiService(IConfiguration configuration)
	{
		_configuration = configuration;
	}

	public async Task EliminaFile()
	{
		var logger = LogManager.Setup()
					   .LoadConfigurationFromAppSettings()
					   .GetCurrentClassLogger();

		// Pulizia file temporanei più vecchi di 1 giorno
		try
		{
			logger.Debug("Pulizia file temporanei piu vecchi di 1 giorno");
			var limite = DateTime.Now.AddDays(-1);

			foreach (var file in Directory.GetFiles(_configuration["CartellaTemporanea"], "*.tmp"))
			{
				var info = new FileInfo(file);

				if (info.LastWriteTime < limite)
				{
					info.Delete();
				}
			}
		}
		catch (Exception ex)
		{
			logger.Error(ex, "Errore durante la pulizia della cartella temporanea");
		}
	}
}
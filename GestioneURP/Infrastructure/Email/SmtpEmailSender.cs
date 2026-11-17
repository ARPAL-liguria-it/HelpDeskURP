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

using GestioneURP.Application.Services;
using MailKit.Net.Smtp;
using MailKit.Security;
using MimeKit;

namespace GestioneURP.Infrastructure.Email;

public sealed class SmtpEmailSender
{
	internal async Task InviaAsync(AnteprimaEmail anteprima, string oggetto,
		IReadOnlyList<AllegatoEmailDto> allegati, CancellationToken cancellationToken)
	{
		using var messaggio = new MimeMessage();
		messaggio.From.Add(MailboxAddress.Parse(anteprima.Smtp.From));
		messaggio.To.Add(MailboxAddress.Parse(anteprima.To));
		if (!string.IsNullOrWhiteSpace(anteprima.Smtp.Bcc))
			messaggio.Bcc.Add(MailboxAddress.Parse(anteprima.Smtp.Bcc));
		messaggio.Subject = oggetto;
		var corpo = new BodyBuilder { HtmlBody = anteprima.Html };
		messaggio.Body = corpo.ToMessageBody();
		foreach (var allegato in allegati)
		{
			await using var file = File.OpenRead(allegato.Percorso);
			await corpo.Attachments.AddAsync(allegato.NomeFile, file, cancellationToken);
			messaggio.Body = corpo.ToMessageBody();
		}

		using var client = new SmtpClient { Timeout = 10_000 };
		await ConTimeoutAsync(ct => client.ConnectAsync(anteprima.Smtp.Server, anteprima.Smtp.Port, SecureSocketOptions.StartTls, ct), cancellationToken);
		await ConTimeoutAsync(ct => client.AuthenticateAsync(anteprima.Smtp.Username, anteprima.Smtp.Password, ct), cancellationToken);
		await ConTimeoutAsync(ct => client.SendAsync(messaggio, ct), cancellationToken);
		// Il successo è l'accettazione SMTP: nessun QUIT o controllo di consegna può cambiarne l'esito.
	}

	private static async Task ConTimeoutAsync(Func<CancellationToken, Task> operazione, CancellationToken cancellationToken)
	{
		using var timeout = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
		timeout.CancelAfter(TimeSpan.FromSeconds(10));
		await operazione(timeout.Token);
	}
}

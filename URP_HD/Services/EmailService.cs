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

using MailKit.Net.Smtp;
using MailKit.Security;
using MimeKit;
using URP_HD.Models;

public class EmailService
{
	private readonly IConfiguration _configuration;

	public EmailService(IConfiguration configuration)
	{
		_configuration = configuration;
	}

	public async Task SendAsync(string destinatario,
		string oggetto,
		string corpo,
		IEnumerable<EmailAttachment>? allegati = null)
	{
		var email = new MimeMessage();

		email.From.Add(new MailboxAddress(
			"ARPAL",
			_configuration["Email:From"]
		));

		email.To.Add(MailboxAddress.Parse(destinatario));

		email.Subject = oggetto;

		if (!string.IsNullOrWhiteSpace(_configuration["Email:Bcc"]))
		{
			email.Bcc.Add(MailboxAddress.Parse(_configuration["Email:Bcc"]));
		}

		// Corpo + allegati
		var multipart = new Multipart("mixed");

		// Corpo della mail
		multipart.Add(new TextPart("html")
		{
			Text = corpo
		});

		// Allegati
		if (allegati != null)
		{
			foreach (var allegato in allegati)
			{
				var attachment = new MimePart(allegato.ContentType)
				{
					Content = new MimeContent(
						new MemoryStream(allegato.Content)
					),
					ContentDisposition = new ContentDisposition(
						ContentDisposition.Attachment
					),
					ContentTransferEncoding = ContentEncoding.Base64,
					FileName = allegato.FileName
				};
				multipart.Add(attachment);
			}
		}

		email.Body = multipart;

		using var smtp = new SmtpClient();

		smtp.Timeout = 10000;

		await smtp.ConnectAsync(
			_configuration["Email:SmtpServer"],
			int.Parse(_configuration["Email:Port"]!),
			SecureSocketOptions.StartTls
		);

		await smtp.AuthenticateAsync(
			_configuration["Email:Username"],
			_configuration["Email:Password"]
		);

		await smtp.SendAsync(email);

		await smtp.DisconnectAsync(true);
	}
}

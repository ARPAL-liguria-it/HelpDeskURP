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

using MimeKit.Text;
using System.Text;

namespace GestioneURP.Infrastructure.Email;

internal static class EmailHtmlText
{
	internal static string Converti(string html)
	{
		using var reader = new StringReader(html);
		var tokenizer = new HtmlTokenizer(reader) { DecodeCharacterReferences = true };
		var testo = new StringBuilder();
		string? ignorato = null;
		var preformattato = false;
		while (tokenizer.ReadNextToken(out var token))
		{
			if (token is HtmlTagToken tag)
			{
				var nome = tag.Name.ToLowerInvariant();
				if (ignorato is not null)
				{
					if (tag.IsEndTag && nome == ignorato) ignorato = null;
					continue;
				}
				if (!tag.IsEndTag && nome is "head" or "script" or "style" or "title")
				{
					ignorato = nome;
					continue;
				}
				if (nome == "pre") preformattato = !tag.IsEndTag;
				if (nome == "br") ACapo(testo, true);
				else if (nome is "p" or "div" or "pre" or "li" or "ul" or "ol" or "tr" or "table"
					or "h1" or "h2" or "h3" or "h4" or "h5" or "h6" or "section" or "article"
					or "header" or "footer" or "main" or "blockquote" or "hr" or "address") ACapo(testo, false);
				else if (nome is "td" or "th" && tag.IsEndTag) testo.Append(' ');
			}
			else if (ignorato is null && token is HtmlDataToken dati)
			{
				if (preformattato) testo.Append(dati.Data);
				else
				{
					foreach (var carattere in dati.Data)
					{
						if (!char.IsWhiteSpace(carattere)) testo.Append(carattere);
						else if (testo.Length > 0 && !char.IsWhiteSpace(testo[^1])) testo.Append(' ');
					}
				}
			}
		}
		return testo.ToString().Trim();
	}

	private static void ACapo(StringBuilder testo, bool esplicito)
	{
		while (testo.Length > 0 && testo[^1] == ' ') testo.Length--;
		if (esplicito || (testo.Length > 0 && testo[^1] != '\n')) testo.Append('\n');
	}
}

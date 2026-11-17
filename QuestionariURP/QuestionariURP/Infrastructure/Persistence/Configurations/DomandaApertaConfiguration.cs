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

using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;
using QuestionariURP.Domain.Entities;

namespace QuestionariURP.Infrastructure.Persistence.Configurations;

internal class DomandaApertaConfiguration : IEntityTypeConfiguration<DomandaAperta>
{
	public void Configure(EntityTypeBuilder<DomandaAperta> builder)
	{
		// Converter CHAR(1) 'S'/'N' ↔ bool
		var ynConverter = new ValueConverter<bool, string>(
			v => v ? "S" : "N",
			v => v == "S");

		builder.Property(d => d.Obbligatoria)
			.HasColumnName("OBBLIGATORIA")
			.HasColumnType("CHAR(1)")
			.HasConversion(ynConverter)
			.IsRequired();
	}
}

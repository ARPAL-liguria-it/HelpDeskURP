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
using QuestionariURP.Domain.Entities;

namespace QuestionariURP.Infrastructure.Persistence.Configurations;

internal class DomandaConfiguration : IEntityTypeConfiguration<Domanda>
{
	public void Configure(EntityTypeBuilder<Domanda> builder)
	{
		builder.ToTable("QUESTIONARI_DOMANDA", "URP_HD");

		builder.HasKey(d => d.Id);
		builder.Property(d => d.Id)
			.HasColumnName("ID")
			.HasColumnType("NUMBER")
			.ValueGeneratedOnAdd();

		builder.Property(d => d.TipoQuestionarioCodice)
			.HasColumnName("TIPO_QUESTIONARIO_CODICE")
			.HasColumnType("VARCHAR2(50)")
			.IsRequired();

		builder.Property(d => d.Ordine)
			.HasColumnName("ORDINE")
			.HasColumnType("NUMBER")
			.IsRequired();

		builder.Property(d => d.Testo)
			.HasColumnName("TESTO")
			.HasColumnType("VARCHAR2(2000)")
			.IsRequired();

		// TPH: discriminatore sulla colonna TIPO_DOMANDA
		builder.HasDiscriminator<string>("TIPO_DOMANDA")
			.HasValue<DomandaChiusa>("C")
			.HasValue<DomandaAperta>("A");

		builder.Property<string>("TIPO_DOMANDA")
			.HasColumnName("TIPO_DOMANDA")
			.HasColumnType("CHAR(1)")
			.IsRequired();
	}
}

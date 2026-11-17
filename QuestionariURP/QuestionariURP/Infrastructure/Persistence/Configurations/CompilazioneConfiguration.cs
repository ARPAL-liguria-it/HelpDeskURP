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

internal class CompilazioneConfiguration : IEntityTypeConfiguration<Compilazione>
{
	public void Configure(EntityTypeBuilder<Compilazione> builder)
	{
		builder.ToTable("QUESTIONARI_COMPILAZIONE", "URP_HD");

		builder.HasKey(c => c.Id);
		builder.Property(c => c.Id)
			.HasColumnName("ID")
			.HasColumnType("NUMBER")
			.ValueGeneratedOnAdd();

		builder.Property(c => c.TipoQuestionarioCodice)
			.HasColumnName("TIPO_QUESTIONARIO_CODICE")
			.HasColumnType("VARCHAR2(50)")
			.IsRequired();

		builder.Property(c => c.Sorgente)
			.HasColumnName("SORGENTE")
			.HasColumnType("VARCHAR2(100)")
			.IsRequired();

		builder.Property(c => c.DataOraCompletamento)
			.HasColumnName("DATA_ORA_COMPLETAMENTO")
			.HasColumnType("TIMESTAMP")
			.IsRequired();

		builder.HasMany(c => c.Risposte)
			.WithOne()
			.HasForeignKey(r => r.CompilazioneId)
			.IsRequired();
	}
}

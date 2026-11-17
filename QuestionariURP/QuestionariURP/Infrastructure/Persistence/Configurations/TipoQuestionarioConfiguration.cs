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

internal class TipoQuestionarioConfiguration : IEntityTypeConfiguration<TipoQuestionario>
{
	public void Configure(EntityTypeBuilder<TipoQuestionario> builder)
	{
		builder.ToTable("QUESTIONARI_TIPO", "URP_HD");

		builder.HasKey(t => t.Codice);
		builder.Property(t => t.Codice)
			.HasColumnName("CODICE")
			.HasColumnType("VARCHAR2(50)")
			.IsRequired();

		builder.Property(t => t.Nome)
			.HasColumnName("NOME")
			.HasColumnType("VARCHAR2(200)")
			.IsRequired();

		builder.Property(t => t.Descrizione)
			.HasColumnName("DESCRIZIONE")
			.HasColumnType("VARCHAR2(1000)");

		builder.HasMany(t => t.Domande)
			.WithOne()
			.HasForeignKey(d => d.TipoQuestionarioCodice)
			.IsRequired();
	}
}

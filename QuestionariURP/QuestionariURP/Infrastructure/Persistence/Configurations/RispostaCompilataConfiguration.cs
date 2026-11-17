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

internal class RispostaCompilataConfiguration : IEntityTypeConfiguration<RispostaCompilata>
{
	public void Configure(EntityTypeBuilder<RispostaCompilata> builder)
	{
		builder.ToTable("QUESTIONARI_RISPOSTA_COMPILATA", "URP_HD");

		builder.HasKey(r => r.Id);
		builder.Property(r => r.Id)
			.HasColumnName("ID")
			.HasColumnType("NUMBER")
			.ValueGeneratedOnAdd();

		builder.Property(r => r.CompilazioneId)
			.HasColumnName("COMPILAZIONE_ID")
			.HasColumnType("NUMBER")
			.IsRequired();

		builder.Property(r => r.DomandaId)
			.HasColumnName("DOMANDA_ID")
			.HasColumnType("NUMBER")
			.IsRequired();

		builder.Property(r => r.OpzioneRispostaId)
			.HasColumnName("OPZIONE_RISPOSTA_ID")
			.HasColumnType("NUMBER");

		builder.Property(r => r.CodiceNumericoSnapshot)
			.HasColumnName("CODICE_NUMERICO_SNAPSHOT")
			.HasColumnType("NUMBER");

		builder.Property(r => r.TestoRisposta)
			.HasColumnName("TESTO_RISPOSTA")
			.HasColumnType("VARCHAR2(4000)");
	}
}

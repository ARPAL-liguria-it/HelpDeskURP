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

internal class OpzioneRispostaConfiguration : IEntityTypeConfiguration<OpzioneRisposta>
{
	public void Configure(EntityTypeBuilder<OpzioneRisposta> builder)
	{
		builder.ToTable("QUESTIONARI_OPZIONE_RISPOSTA", "URP_HD");

		builder.HasKey(o => o.Id);
		builder.Property(o => o.Id)
			.HasColumnName("ID")
			.HasColumnType("NUMBER")
			.ValueGeneratedOnAdd();

		builder.Property(o => o.DomandaId)
			.HasColumnName("DOMANDA_ID")
			.HasColumnType("NUMBER")
			.IsRequired();

		builder.Property(o => o.Posizione)
			.HasColumnName("POSIZIONE")
			.HasColumnType("NUMBER")
			.IsRequired();

		builder.Property(o => o.CodiceNumerico)
			.HasColumnName("CODICE_NUMERICO")
			.HasColumnType("NUMBER")
			.IsRequired();

		builder.Property(o => o.Testo)
			.HasColumnName("TESTO")
			.HasColumnType("VARCHAR2(500)")
			.IsRequired();
	}
}

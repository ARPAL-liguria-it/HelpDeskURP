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

using System;
using System.Collections.Generic;
using Microsoft.EntityFrameworkCore;

namespace URP_HD.Data;

public partial class URP_HDContext : DbContext
{
    public URP_HDContext()
    {
    }

    public URP_HDContext(DbContextOptions<URP_HDContext> options)
        : base(options)
    {
    }

    public virtual DbSet<ALLEGATI_RECLAMI> ALLEGATI_RECLAMI { get; set; }

    public virtual DbSet<ALLEGATI_RICHIESTE> ALLEGATI_RICHIESTE { get; set; }

    public virtual DbSet<CONTATORI> CONTATORI { get; set; }

    public virtual DbSet<RECLAMI> RECLAMI { get; set; }

    public virtual DbSet<RICHIESTE> RICHIESTE { get; set; }

    public virtual DbSet<TIPI_AUTORE_RICHIESTA> TIPI_AUTORE_RICHIESTA { get; set; }

    public virtual DbSet<TIPI_LUOGO> TIPI_LUOGO { get; set; }

    public virtual DbSet<TIPI_RICHIESTA> TIPI_RICHIESTA { get; set; }

    public virtual DbSet<TIPI_SORGENTE> TIPI_SORGENTE { get; set; }

    public virtual DbSet<TIPI_STRUTTURA> TIPI_STRUTTURA { get; set; }

    public virtual DbSet<TIPI_TEMATICA> TIPI_TEMATICA { get; set; }

    public virtual DbSet<V_ELENCO_COMUNI_PROV> V_ELENCO_COMUNI_PROV { get; set; }

	//    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
	//#warning To protect potentially sensitive information in your connection string, you should move it out of source code. You can avoid scaffolding the connection string by using the Name= syntax to read it from configuration - see https://go.microsoft.com/fwlink/?linkid=2131148. For more guidance on storing connection strings, see https://go.microsoft.com/fwlink/?LinkId=723263.
	//        => optionsBuilder.UseOracle("user id=URP_HD;password=URP_HD;data source=localhost/xe");

	protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
	{
		if (!optionsBuilder.IsConfigured)
		{
			// Lascia vuoto oppure configura solo per scenari particolari.
		}
	}

	protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder
            .HasDefaultSchema("URP_HD")
            .UseCollation("USING_NLS_COMP");

        modelBuilder.Entity<ALLEGATI_RECLAMI>(entity =>
        {
            entity.HasKey(e => e.ID_ALLEGATO).HasName("ALLEGATI_RECLAMI_PK");

            entity.Property(e => e.ID_ALLEGATO).HasPrecision(10);
            entity.Property(e => e.ID_RECLAMO).HasPrecision(10);
            entity.Property(e => e.NOME_FILE)
                .HasMaxLength(200)
                .IsUnicode(false);
            entity.Property(e => e.SORGENTE)
                .HasMaxLength(20)
                .IsUnicode(false);

            entity.HasOne(d => d.ID_RECLAMONavigation).WithMany(p => p.ALLEGATI_RECLAMI)
                .HasForeignKey(d => d.ID_RECLAMO)
                .HasConstraintName("ALLEGATI_RECLAMI_FK1");
        });

        modelBuilder.Entity<ALLEGATI_RICHIESTE>(entity =>
        {
            entity.HasKey(e => e.ID_ALLEGATO).HasName("ALLEGATI_RICHIESTE_PK");

            entity.Property(e => e.ID_ALLEGATO).HasPrecision(10);
            entity.Property(e => e.ID_RICHIESTA).HasPrecision(10);
            entity.Property(e => e.NOME_FILE)
                .HasMaxLength(200)
                .IsUnicode(false);
            entity.Property(e => e.SORGENTE)
                .HasMaxLength(20)
                .IsUnicode(false);

            entity.HasOne(d => d.ID_RICHIESTANavigation).WithMany(p => p.ALLEGATI_RICHIESTE)
                .HasForeignKey(d => d.ID_RICHIESTA)
                .HasConstraintName("ALLEGATI_RICHIESTE_FK1");
        });

        modelBuilder.Entity<CONTATORI>(entity =>
        {
            entity.HasKey(e => e.CONTATORE).HasName("CONTATORI_PK");

            entity.Property(e => e.CONTATORE)
                .HasMaxLength(20)
                .IsUnicode(false);
            entity.Property(e => e.VALORE)
                .HasPrecision(10)
                .HasDefaultValueSql("0 ");
        });

        modelBuilder.Entity<RECLAMI>(entity =>
        {
            entity.HasKey(e => e.ID_RECLAMO).HasName("RECLAMI_PK");

            entity.HasIndex(e => e.STATO_PRATICA, "IX_RECLAMI_STATO_PRATICA");

            entity.Property(e => e.ID_RECLAMO).HasPrecision(10);
            entity.Property(e => e.ANNOTAZIONI_ASSEGNAZIONE)
                .HasMaxLength(1000)
                .IsUnicode(false);
            entity.Property(e => e.ANNOTAZIONI_CHIUSURA)
                .HasMaxLength(1000)
                .IsUnicode(false);
            entity.Property(e => e.ANNOTAZIONI_INOLTRO)
                .HasMaxLength(1000)
                .IsUnicode(false);
            entity.Property(e => e.ANNOTAZIONI_PRESA_IN_CARICO)
                .HasMaxLength(1000)
                .IsUnicode(false);
            entity.Property(e => e.ANNOTAZIONI_RISPOSTA)
                .HasMaxLength(1000)
                .IsUnicode(false);
            entity.Property(e => e.CODICE)
                .HasMaxLength(20)
                .IsUnicode(false);
            entity.Property(e => e.CODICE_STRUTTURA)
                .HasMaxLength(20)
                .IsUnicode(false);
            entity.Property(e => e.COGNOME_AUTORE)
                .HasMaxLength(200)
                .IsUnicode(false);
            entity.Property(e => e.DATA_ASSEGNAZIONE_ALTRO_OPERATORE).HasColumnType("DATE");
            entity.Property(e => e.DATA_CHIUSURA).HasColumnType("DATE");
            entity.Property(e => e.DATA_INOLTRO_ALTRA_STRUTTURA).HasColumnType("DATE");
            entity.Property(e => e.DATA_INSERIMENTO)
                .HasDefaultValueSql("sysdate ")
                .HasColumnType("DATE");
            entity.Property(e => e.DATA_PRESA_IN_CARICO).HasColumnType("DATE");
            entity.Property(e => e.DATA_RISPOSTA).HasColumnType("DATE");
            entity.Property(e => e.DATA_RISPOSTA_STRUTTURA).HasColumnType("DATE");
            entity.Property(e => e.ELIMINATO)
                .HasMaxLength(1)
                .IsUnicode(false)
                .HasDefaultValueSql("'N' ")
                .IsFixedLength();
            entity.Property(e => e.EMAIL_AUTORE)
                .HasMaxLength(200)
                .IsUnicode(false);
            entity.Property(e => e.ID_OPERATORE_ASSEGNATARIO).HasPrecision(10);
            entity.Property(e => e.ID_OPERATORE_PRESA_IN_CARICO).HasPrecision(10);
            entity.Property(e => e.ID_TIPO_AUTORE_RICHIESTA).HasPrecision(10);
            entity.Property(e => e.ID_TIPO_RICHIESTA).HasPrecision(10);
            entity.Property(e => e.NOME_AUTORE)
                .HasMaxLength(200)
                .IsUnicode(false);
            entity.Property(e => e.OPERATORE_ASSEGNATARIO)
                .HasMaxLength(200)
                .IsUnicode(false);
            entity.Property(e => e.OPERATORE_PRESA_IN_CARICO)
                .HasMaxLength(200)
                .IsUnicode(false);
            entity.Property(e => e.OPERATORE_ULTIMO_AGGIORNAMENTO)
                .HasMaxLength(100)
                .IsUnicode(false);
            entity.Property(e => e.PEC_AUTORE)
                .HasMaxLength(200)
                .IsUnicode(false);
            entity.Property(e => e.RAGIONE_SOCIALE)
                .HasMaxLength(200)
                .IsUnicode(false);
            entity.Property(e => e.RISPOSTA_DA_STRUTTURA)
                .HasMaxLength(2000)
                .IsUnicode(false);
            entity.Property(e => e.STATO_PRATICA)
                .HasMaxLength(64)
                .IsUnicode(false)
                .HasDefaultValueSql("'Aperta'");
            entity.Property(e => e.STRUTTURA_DESTINATARIA)
                .HasMaxLength(500)
                .IsUnicode(false);
            entity.Property(e => e.TELEFONO_AUTORE)
                .HasMaxLength(200)
                .IsUnicode(false);
            entity.Property(e => e.TESTO)
                .HasMaxLength(3000)
                .IsUnicode(false);
            entity.Property(e => e.TESTO_RISPOSTA).IsUnicode(false);
            entity.Property(e => e.TIPO_SORGENTE)
                .HasMaxLength(20)
                .IsUnicode(false);
            entity.Property(e => e.ULTIMO_AGGIORNAMENTO).HasColumnType("DATE");

            entity.HasOne(d => d.CODICE_STRUTTURANavigation).WithMany(p => p.RECLAMI)
                .HasForeignKey(d => d.CODICE_STRUTTURA)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("RECLAMI_FK3");

            entity.HasOne(d => d.ID_TIPO_AUTORE_RICHIESTANavigation).WithMany(p => p.RECLAMI)
                .HasForeignKey(d => d.ID_TIPO_AUTORE_RICHIESTA)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("RECLAMI_FK2");

            entity.HasOne(d => d.ID_TIPO_RICHIESTANavigation).WithMany(p => p.RECLAMI)
                .HasForeignKey(d => d.ID_TIPO_RICHIESTA)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("RECLAMI_FK1");

            entity.HasOne(d => d.TIPO_SORGENTENavigation).WithMany(p => p.RECLAMI)
                .HasForeignKey(d => d.TIPO_SORGENTE)
                .HasConstraintName("RECLAMI_FK4");
        });

        modelBuilder.Entity<RICHIESTE>(entity =>
        {
            entity.HasKey(e => e.ID_RICHIESTA).HasName("RICHIESTE_PK");

            entity.HasIndex(e => e.STATO_PRATICA, "IX_RICHIESTE_STATO_PRATICA");

            entity.Property(e => e.ID_RICHIESTA).HasPrecision(10);
            entity.Property(e => e.ANNOTAZIONI_ASSEGNAZIONE)
                .HasMaxLength(1000)
                .IsUnicode(false);
            entity.Property(e => e.ANNOTAZIONI_CHIUSURA)
                .HasMaxLength(1000)
                .IsUnicode(false);
            entity.Property(e => e.ANNOTAZIONI_INOLTRO)
                .HasMaxLength(1000)
                .IsUnicode(false);
            entity.Property(e => e.ANNOTAZIONI_PRESA_IN_CARICO)
                .HasMaxLength(1000)
                .IsUnicode(false);
            entity.Property(e => e.ANNOTAZIONI_RISPOSTA)
                .HasMaxLength(1000)
                .IsUnicode(false);
            entity.Property(e => e.CODICE)
                .HasMaxLength(20)
                .IsUnicode(false);
            entity.Property(e => e.COGNOME_AUTORE)
                .HasMaxLength(200)
                .IsUnicode(false);
            entity.Property(e => e.COMUNE_LUOGO)
                .HasMaxLength(200)
                .IsUnicode(false);
            entity.Property(e => e.DATA_ASSEGNAZIONE_ALTRO_OPERATORE).HasColumnType("DATE");
            entity.Property(e => e.DATA_CHIUSURA).HasColumnType("DATE");
            entity.Property(e => e.DATA_INOLTRO_ALTRA_STRUTTURA).HasColumnType("DATE");
            entity.Property(e => e.DATA_INSERIMENTO)
                .HasDefaultValueSql("SYSDATE ")
                .HasColumnType("DATE");
            entity.Property(e => e.DATA_PRESA_IN_CARICO).HasColumnType("DATE");
            entity.Property(e => e.DATA_RISPOSTA).HasColumnType("DATE");
            entity.Property(e => e.DATA_RISPOSTA_STRUTTURA).HasColumnType("DATE");
            entity.Property(e => e.ELIMINATA)
                .HasMaxLength(1)
                .IsUnicode(false)
                .HasDefaultValueSql("'N' ")
                .IsFixedLength();
            entity.Property(e => e.EMAIL_AUTORE)
                .HasMaxLength(200)
                .IsUnicode(false);
            entity.Property(e => e.ID_OPERATORE_ASSEGNATARIO).HasPrecision(10);
            entity.Property(e => e.ID_OPERATORE_PRESA_IN_CARICO).HasPrecision(10);
            entity.Property(e => e.ID_TIPO_AUTORE_RICHIESTA).HasPrecision(10);
            entity.Property(e => e.ID_TIPO_LUOGO).HasPrecision(10);
            entity.Property(e => e.ID_TIPO_RICHIESTA).HasPrecision(10);
            entity.Property(e => e.ID_TIPO_TEMATICA).HasPrecision(10);
            entity.Property(e => e.INDIRIZZO)
                .HasMaxLength(500)
                .IsUnicode(false);
            entity.Property(e => e.NOME_AUTORE)
                .HasMaxLength(200)
                .IsUnicode(false);
            entity.Property(e => e.OPERATORE_ASSEGNATARIO)
                .HasMaxLength(200)
                .IsUnicode(false);
            entity.Property(e => e.OPERATORE_PRESA_IN_CARICO)
                .HasMaxLength(200)
                .IsUnicode(false);
            entity.Property(e => e.OPERATORE_ULTIMO_AGGIORNAMENTO)
                .HasMaxLength(100)
                .IsUnicode(false);
            entity.Property(e => e.PEC_AUTORE)
                .HasMaxLength(200)
                .IsUnicode(false);
            entity.Property(e => e.RAGIONE_SOCIALE)
                .HasMaxLength(200)
                .IsUnicode(false);
            entity.Property(e => e.RISPOSTA_DA_STRUTTURA)
                .HasMaxLength(2000)
                .IsUnicode(false);
            entity.Property(e => e.STATO_PRATICA)
                .HasMaxLength(64)
                .IsUnicode(false)
                .HasDefaultValueSql("'Aperta'");
            entity.Property(e => e.STRUTTURA_DESTINATARIA)
                .HasMaxLength(500)
                .IsUnicode(false);
            entity.Property(e => e.TELEFONO_AUTORE)
                .HasMaxLength(200)
                .IsUnicode(false);
            entity.Property(e => e.TESTO)
                .HasMaxLength(3000)
                .IsUnicode(false);
            entity.Property(e => e.TESTO_RISPOSTA).IsUnicode(false);
            entity.Property(e => e.TIPO_SORGENTE)
                .HasMaxLength(20)
                .IsUnicode(false);
            entity.Property(e => e.ULTIMO_AGGIORNAMENTO).HasColumnType("DATE");

            entity.HasOne(d => d.ID_TIPO_AUTORE_RICHIESTANavigation).WithMany(p => p.RICHIESTE)
                .HasForeignKey(d => d.ID_TIPO_AUTORE_RICHIESTA)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("RICHIESTE_FK2");

            entity.HasOne(d => d.ID_TIPO_LUOGONavigation).WithMany(p => p.RICHIESTE)
                .HasForeignKey(d => d.ID_TIPO_LUOGO)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("RICHIESTE_FK4");

            entity.HasOne(d => d.ID_TIPO_RICHIESTANavigation).WithMany(p => p.RICHIESTE)
                .HasForeignKey(d => d.ID_TIPO_RICHIESTA)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("RICHIESTE_FK1");

            entity.HasOne(d => d.ID_TIPO_TEMATICANavigation).WithMany(p => p.RICHIESTE)
                .HasForeignKey(d => d.ID_TIPO_TEMATICA)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("RICHIESTE_FK3");

            entity.HasOne(d => d.TIPO_SORGENTENavigation).WithMany(p => p.RICHIESTE)
                .HasForeignKey(d => d.TIPO_SORGENTE)
                .HasConstraintName("RICHIESTE_FK5");
        });

        modelBuilder.Entity<TIPI_AUTORE_RICHIESTA>(entity =>
        {
            entity.HasKey(e => e.ID_TIPO_AUTORE_RICHIESTA).HasName("TIPI_AUTORE_RICHIESTA_PK");

            entity.Property(e => e.ID_TIPO_AUTORE_RICHIESTA)
                .HasPrecision(10)
                .ValueGeneratedNever();
            entity.Property(e => e.DESCRIZIONE_TIPO_AUTORE_RICHIESTA)
                .HasMaxLength(50)
                .IsUnicode(false);
            entity.Property(e => e.ORDINE)
                .HasPrecision(10)
                .HasDefaultValueSql("0 ");
        });

        modelBuilder.Entity<TIPI_LUOGO>(entity =>
        {
            entity.HasKey(e => e.ID_TIPO_LUOGO).HasName("TIPI_LUOGO_PK");

            entity.Property(e => e.ID_TIPO_LUOGO)
                .HasPrecision(10)
                .ValueGeneratedNever();
            entity.Property(e => e.DESCRIZIONE_TIPO_LUOGO)
                .HasMaxLength(50)
                .IsUnicode(false);
            entity.Property(e => e.ORDINE)
                .HasPrecision(10)
                .HasDefaultValueSql("0 ");
        });

        modelBuilder.Entity<TIPI_RICHIESTA>(entity =>
        {
            entity.HasKey(e => e.ID_TIPO_RICHIESTA).HasName("TIPI_RICHIESTA_PK");

            entity.Property(e => e.ID_TIPO_RICHIESTA)
                .HasPrecision(10)
                .ValueGeneratedNever();
            entity.Property(e => e.DESCRIZIONE_TIPO_RICHIESTA)
                .HasMaxLength(100)
                .IsUnicode(false);
            entity.Property(e => e.DETTAGLIO)
                .HasMaxLength(200)
                .IsUnicode(false);
            entity.Property(e => e.ORDINE)
                .HasPrecision(10)
                .HasDefaultValueSql("0 ");
        });

        modelBuilder.Entity<TIPI_SORGENTE>(entity =>
        {
            entity.HasKey(e => e.TIPO_SORGENTE).HasName("TIPI_SORGENTE_PK");

            entity.Property(e => e.TIPO_SORGENTE)
                .HasMaxLength(20)
                .IsUnicode(false);
            entity.Property(e => e.ORDINE)
                .HasPrecision(10)
                .HasDefaultValueSql("0 ");
        });

        modelBuilder.Entity<TIPI_STRUTTURA>(entity =>
        {
            entity.HasKey(e => e.CODICE_STRUTTURA).HasName("TIPI_STRUTTURA_PK");

            entity.Property(e => e.CODICE_STRUTTURA)
                .HasMaxLength(20)
                .IsUnicode(false);
            entity.Property(e => e.DESCRIZIONE_STRUTTURA)
                .HasMaxLength(200)
                .IsUnicode(false);
            entity.Property(e => e.ORDINE)
                .HasPrecision(10)
                .HasDefaultValueSql("0 ");
        });

        modelBuilder.Entity<TIPI_TEMATICA>(entity =>
        {
            entity.HasKey(e => e.ID_TIPO_TEMATICA).HasName("TIPI_TEMATICA_PK");

            entity.Property(e => e.ID_TIPO_TEMATICA)
                .HasPrecision(10)
                .ValueGeneratedNever();
            entity.Property(e => e.DESCRIZIONE_TIPO_TEMATICA)
                .HasMaxLength(50)
                .IsUnicode(false);
            entity.Property(e => e.ORDINE)
                .HasPrecision(10)
                .HasDefaultValueSql("0 ");
        });

        modelBuilder.Entity<V_ELENCO_COMUNI_PROV>(entity =>
        {
            entity
                .HasNoKey()
                .ToView("V_ELENCO_COMUNI_PROV");

            entity.Property(e => e.DESC_COMUNE_PROVINCIA)
                .HasMaxLength(55)
                .IsUnicode(false);
        });
        modelBuilder.HasSequence("ALLEGATI_RECLAMI_SEQ");
        modelBuilder.HasSequence("ALLEGATI_RICHIESTE_SEQ");
        modelBuilder.HasSequence("RECLAMI_SEQ");
        modelBuilder.HasSequence("RICHIESTE_SEQ");

        OnModelCreatingPartial(modelBuilder);
    }

    partial void OnModelCreatingPartial(ModelBuilder modelBuilder);
}

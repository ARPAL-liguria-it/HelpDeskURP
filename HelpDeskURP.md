# ***Helpdesk URP***

# Sommario

[1. Dipendenze	3](#_Toc245192999)

[2. Installazione ambiente di sviluppo	3](#_Toc245193000)

[3. Compilazione software	3](#_Toc245193001)

[4. Installazione software	3](#_Toc245193002)

[Creazione database	3](#_Toc245193003)

[Applicazioni	3](#_Toc245193004)

[Descrizione file di configurazione	4](#_Toc245193005)

[Procedura di verifica dell’installazione	5](#_Toc245193006)

[5. Descrizione architettura	7](#_Toc245193007)

[Applicazioni Front-End	7](#_Toc245193008)

[Base dati Oracle	7](#_Toc245193009)

[Cartelle archivio file	7](#_Toc245193010)

[Ciclo di vita di una richiesta	7](#_Toc245193011)

[6. Modello dati	8](#_Toc245193012)

[RICHIESTE	8](#_Toc245193013)

[RECLAMI	9](#_Toc245193014)

[ALLEGATI\_RECLAMI	9](#_Toc245193015)

[ALLEGATI\_RICHIESTE	10](#_Toc245193016)

[COMUNI	10](#_Toc245193017)

[CONTATORI	10](#_Toc245193018)

[QUESTIONARI\_COMPILAZIONE	10](#_Toc245193019)

[QUESTIONARI\_DOMANDA	10](#_Toc245193020)

[QUESTIONARI\_OPZIONE\_RISPOSTA	10](#_Toc245193021)

[QUESTIONARI\_RISPOSTA\_COMPILATA	11](#_Toc245193022)

[QUESTIONARI\_SORGENTE	11](#_Toc245193023)

[QUESTIONARI\_TIPO	11](#_Toc245193024)

[QUESTIONARI\_TOKEN	11](#_Toc245193025)

[REGISTRO\_ATTIVITA	11](#_Toc245193026)

[TIPI\_AUTORE\_RICHIESTA	12](#_Toc245193027)

[TIPI\_LUOGO	12](#_Toc245193028)

[TIPI\_RICHIESTA	12](#_Toc245193029)

[TIPI\_SORGENTE	12](#_Toc245193030)

[TIPI\_STRUTTURA	12](#_Toc245193031)

[TIPI\_TEMATICA	12](#_Toc245193032)

[8. Dati da servizi esterni	13](#_Toc245193033)

[Logon	13](#_Toc245193034)

[GetAppUserProfiles	14](#_Toc245193035)

[GetApplicationUsers	16](#_Toc245193036)

[GetAllStrutture	18](#_Toc245193037)



## 1. Dipendenze

L'applicazione è composta di più componenti che richiedono:

Server:

- Sistema operativo Microsoft Windows Server 2019 o successivo

- Microsoft IIS versione 8.5 o successiva

- Microsoft .NET 10

- Oracle Database v.21c o successiva

- API per autenticazione e dati di servizio

Client:

- Browser Web

- Accesso con reverse proxy con autenticazione

## 2. Installazione ambiente di sviluppo

L'ambiente di sviluppo è Microsoft Visual Studio 2026, da installare con strumenti di sviluppo C\# e .NET.

[https://visualstudio.microsoft.com/it/](https://visualstudio.microsoft.com/it/)

E' possibile utilizzare anche Microsoft Visual Studio Code

[https://code.visualstudio.com/](https://code.visualstudio.com/)

E' richiesta l'installazione di Oracle Developer Tools per Visual Studio

[https://www.oracle.com/database/technologies/developer-tools/visual-studio/](https://www.oracle.com/database/technologies/developer-tools/visual-studio/)

## 3. Compilazione software

Il software comprende tre progetti di Visual Studio:

- URP\_HD: applicazione Web front office per i cittadini per l’inserimento richieste per l’ufficio relazioni con il pubblico;

- QuestionariURP: applicazione Web front office per i cittadini per la compilazione questionari di gradimento;

- GestioneURP: applicazione Web back office per gli operatori dell’ufficio relazioni con il pubblico per la gestione delle richieste.

I progetti sono compilabili per il rilascio mediante la funzione di Visual Studio (Menu Compilazione-\>Pubblica)

## 4. Installazione software

### Creazione database

Creare uno schema Oracle ed eseguire gli script forniti.

### Applicazioni

Per ciascuna applicazione seguire la seguente procedura. Ogni applicazione va installata in una diversa cartella e per ognuna va configurato un diverso sito Web.

Copiare la versione compilata in una cartella sul server Web. 

Da interfaccia di IIS creare un application pool con nessun codice gestito.

Creare un sito Web che utilizza l'application pool al punto precedente e che punta alla relativa cartella sul server Web.

Configurare il sito per utilizzare una connessione protetta in https con certificato SSL.

L’account che esegue IIS deve poter accedere in scrittura alle cartelle Log e a quelle indicate nei file di configurazione di ciascuna applicazione.

### Descrizione file di configurazione

La configurazione di ogni applicazione è contenuta nel rispettivo file appsettings.json.

Per tutte le applicazioni è inserita una configurazione standard di Nlog per il log in locale e via e-mail; la sintassi è disponibile su https://nlog-project.org/config

#### *Applicazione URP\_HD*

| **Impostazione** | **Descrizione** |
| - | - |
| StringaConnessioneDB | Stringa di connessione per l'accesso al database Oracle |
| CartellaTemporanea | Percorso completo della cartella per i file temporanei dell’applicazione; deve essere diversa da quelle utilizzata dalle altre applicazioni |
| CartellaAllegatiRichieste | Percorso completo della cartella per i file allegati alle richieste |
| CartellaAllegatiReclami | Percorso completo della cartella per i file allegati ai reclami |
| EstensioniAllegati | Elenco delle estensioni accettate per il caricamento di allegati separate da virgola |
| URLPrivacy | URL della pagina dell’ente con informativa sulla privacy |
| URLAmministrazioneTrasparente | URL della pagina dell’ente dedicata all’amministrazione trasparente |
| Email | Configurazione del server SMTP per l’invio di email in seguito a inserimento di una richiesta al cittadino e all’URP |
| HeaderAutenticazione | Header HTTP passati dal sistema di autenticazione |


#### *Applicazione QuestionariURP*

| **Impostazione** | **Descrizione** |
| - | - |
| CartellaTemporanea | Percorso completo della cartella per i file temporanei dell’applicazione; deve essere diversa da quelle utilizzata dalle altre applicazioni |


#### *Applicazione GestioneURP*

| **Impostazione** | **Descrizione** |
| - | - |
| StringaConnessioneDB | Stringa di connessione per l'accesso al database Oracle |
| CartellaTemporanea | Percorso completo della cartella per i file temporanei dell’applicazione; deve essere diversa da quelle utilizzata dalle altre applicazioni |
| CartellaAllegatiRichieste | Percorso completo della cartella per i file allegati alle richieste; deve essere la stessa utilizzata da URP\_HD |
| CartellaAllegatiReclami | Percorso completo della cartella per i file allegati ai reclami; deve essere la stessa utilizzata da URP\_HD |
| EstensioniAllegati | Elenco delle estensioni accettate per il caricamento di allegati separate da virgola |
| DimensioneMassimaAllegatoMB | Dimensione massima di un file caricabile come allegato |
| NumeroMassimoAllegati | Numero massimo di allegati caricabili |
| GsoWsBaseUrl | URL del sistema di autenticazione |
| IdentificativoApplicazione |  |
| QuestionariBaseUrl |  |
| Email | Configurazione del server SMTP per l’invio di email al cittadino e all’URP e codice del questionario di gradimento da inviare con la risposta |

### Procedura di verifica dell’installazione

Di seguito è indicato come testare l’installazione, e completare un ciclo di gestione richiesta per verificare che tutte le funzionalità siano operative.

Separatamente verificare che:

- le applicazioni siano raggiungibili tramite browser;

- la base dati sia raggiungibile dalle applicazioni;

- le applicazioni URP\_HD e GestioneURP possano accedere in lettura e scrittura alle rispettive cartelle Log, Temporanea e Allegati;

- I servizi esterni (autenticazione, SMTP) siano raggiungibili dalle applicazioni.

I log degli errori JavaScript sono mostrati nella console del browser; quelli code-behind sono archiviati come file nelle rispettive sottocartelle Log. Il servizio usato per la scrittura e archiviazione dei log è NLog le cui impostazioni sono contenute nei file di configurazione delle singole applicazioni.

#### *Scenari di test*

##### 1. Inserimento di una richiesta

- Accedere all’applicazione URP\_HD via browser

- Selezionare un tipo di richiesta

- Seguire i passaggi per l’inserimento.

- Verificare sul database che la richiesta sia stata inserita, che i messaggi email siano arrivati e che gli allegati siano stati salvati sul file system del server.

##### 2. Inserimento di un reclamo

- Accedere alla pagina /autenticazione (vedi sezione Architettura).

- Dopo l’autenticazione selezionare la tipologia reclamo.

- Seguire i passaggi per l’inserimento.

- Verificare sul database che la richiesta sia stata inserita, che i messaggi email siano arrivati e che gli allegati siano stati salvati sul file system del server.

##### 2.Gestione richieste

- Accedere all’applicazione GestioneURP via browser mediante autenticazione (vedi sezione Architettura).

- E' visualizzata la pagina di elenco richieste.

- Selezionare una richiesta ed effettuare i passaggi di presa in carico, risposta, chiusura.

##### 3. Compilazione di un questionario

- Inserire i record necessari in base dati (vedi sezioni Architettura e Modello dati).

- Accedere via browser all’applicazione QuestionariURP con il percorso relativo /questionario/libero?sorgente=\[Nome sorgente\]&codice=\[Codice questionario\] sostituendo i parametri tra parentesi con quelli  configurati in base dati.

- Compilare il questionario.

- Verificare che siano presenti i dati di compilazione in base dati


## 5. Descrizione architettura

L'architettura del sistema si basa su una struttura che integra diverse applicazioni front-end per gestire il ciclo di vita delle richieste e dei questionari e l'integrazione con sistemi esterni.

### Applicazioni Front-End

Interfacce utente per l’inserimento di richieste, la gestione lato back office, la compilazione di questionari, accessibili da qualsiasi dispositivo con un browser.

L’applicazione URP\_HD risponde sia ad accessi anonimi al percorso /, sia autenticati al percorso /autenticazione, dove legge gli header passati da un sistema di autenticazione esterno ricavando nome, cognome e e-mail; se presenti, abilita la compilazione del reclamo, altrimenti delle sole richieste.

L’applicazione QuestionariURP non richiede autenticazione.

L’applicazione GestioneURP utilizza un servizio esterno per l’autenticazione degli operatori (vedi sezione Servizi esterni).

### Base dati Oracle

Archivio centrale per i dati strutturati di richieste e questionari.

### Cartelle archivio file

Conservano gli allegati alle richieste e i file temporanei.

I file allegati sono salvati con nome corrispondente all’id del record inserito nelle tabelle ALLEGATI\_\*.

### Ciclo di vita di una richiesta

Una richiesta può essere inserita da URP\_HD o da GestioneURP.

La richiesta inserita può essere indicata come presa in carico, risposta, assegnata ad altro operatore, assegnata ad altra struttura e chiusa con relativi dati contestuali.


## 6. Modello dati

Di seguito le tabelle contenute nella base dati.

### RICHIESTE

Dati delle richieste.

| **Campo** | **Descrizione** |
| - | - |
| ID\_RICHIESTA | Identificativo del record |
| DATA\_INSERIMENTO |  |
| ID\_TIPO\_RICHIESTA |  |
| ID\_TIPO\_AUTORE\_RICHIESTA |  |
| COGNOME\_AUTORE |  |
| NOME\_AUTORE |  |
| PEC\_AUTORE |  |
| EMAIL\_AUTORE |  |
| TELEFONO\_AUTORE |  |
| ID\_TIPO\_TEMATICA |  |
| ID\_TIPO\_LUOGO |  |
| COMUNE\_LUOGO |  |
| TESTO |  |
| CODICE | Codice della richiesta (RI + valore da tabella contatori) |
| TIPO\_SORGENTE |  |
| INDIRIZZO |  |
| RAGIONE\_SOCIALE |  |
| STATO\_PRATICA |  |
| DATA\_PRESA\_IN\_CARICO |  |
| OPERATORE\_PRESA\_IN\_CARICO |  |
| ID\_OPERATORE\_PRESA\_IN\_CARICO |  |
| ANNOTAZIONI\_PRESA\_IN\_CARICO |  |
| DATA\_ASSEGNAZIONE\_ALTRO\_OPERATORE |  |
| OPERATORE\_ASSEGNATARIO |  |
| ID\_OPERATORE\_ASSEGNATARIO |  |
| ANNOTAZIONI\_ASSEGNAZIONE |  |
| DATA\_INOLTRO\_ALTRA\_STRUTTURA |  |
| STRUTTURA\_DESTINATARIA |  |
| ANNOTAZIONI\_INOLTRO |  |
| DATA\_RISPOSTA\_STRUTTURA |  |
| RISPOSTA\_DA\_STRUTTURA |  |
| DATA\_RISPOSTA |  |
| TESTO\_RISPOSTA |  |
| ANNOTAZIONI\_RISPOSTA |  |
| DATA\_CHIUSURA |  |
| ANNOTAZIONI\_CHIUSURA |  |
| ULTIMO\_AGGIORNAMENTO |  |
| OPERATORE\_ULTIMO\_AGGIORNAMENTO |  |
| ELIMINATA | Indica la cancellazione logica del record |
| CODICE\_STRUTTURA\_DESTINATARIA |  |

### RECLAMI

Dati dei reclami.

| **Campo** | **Descrizione** |
| - | - |
| ID\_RECLAMO | Identificativo del record |
| DATA\_INSERIMENTO |  |
| ID\_TIPO\_RICHIESTA |  |
| ID\_TIPO\_AUTORE\_RICHIESTA |  |
| COGNOME\_AUTORE |  |
| NOME\_AUTORE |  |
| PEC\_AUTORE |  |
| EMAIL\_AUTORE |  |
| TELEFONO\_AUTORE |  |
| TESTO |  |
| CODICE\_STRUTTURA |  |
| CODICE | Codice del reclamo (RE + valore da tabella contatori) |
| TIPO\_SORGENTE |  |
| RAGIONE\_SOCIALE |  |
| STATO\_PRATICA |  |
| DATA\_PRESA\_IN\_CARICO |  |
| OPERATORE\_PRESA\_IN\_CARICO |  |
| ID\_OPERATORE\_PRESA\_IN\_CARICO |  |
| ANNOTAZIONI\_PRESA\_IN\_CARICO |  |
| DATA\_ASSEGNAZIONE\_ALTRO\_OPERATORE |  |
| OPERATORE\_ASSEGNATARIO |  |
| ID\_OPERATORE\_ASSEGNATARIO |  |
| ANNOTAZIONI\_ASSEGNAZIONE |  |
| DATA\_INOLTRO\_ALTRA\_STRUTTURA |  |
| STRUTTURA\_DESTINATARIA |  |
| ANNOTAZIONI\_INOLTRO |  |
| DATA\_RISPOSTA\_STRUTTURA |  |
| RISPOSTA\_DA\_STRUTTURA |  |
| DATA\_RISPOSTA |  |
| TESTO\_RISPOSTA |  |
| ANNOTAZIONI\_RISPOSTA |  |
| DATA\_CHIUSURA |  |
| ANNOTAZIONI\_CHIUSURA |  |
| ULTIMO\_AGGIORNAMENTO |  |
| OPERATORE\_ULTIMO\_AGGIORNAMENTO |  |
| ELIMINATO | Indica la cancellazione logica del record |
| CODICE\_STRUTTURA\_DESTINATARIA |  |

### ALLEGATI\_RECLAMI

Dati dei file allegati ai reclami.

| **Campo** | **Descrizione** |
| - | - |
| ID\_ALLEGATO | Identificativo del record |
| ID\_RECLAMO |  |
| NOME\_FILE |  |
| SORGENTE |  |

### ALLEGATI\_RICHIESTE

Prelevatori associati a un campionamento o selezionabili.

| **Campo** | **Descrizione** |
| - | - |
| ID\_ALLEGATO | Identificativo del record |
| ID\_RICHIESTA |  |
| NOME\_FILE |  |
| SORGENTE |  |

### COMUNI

Elenco dei comuni.

| **Campo** | **Descrizione** |
| - | - |
| CODICE\_COMUNE | Identificativo del record |
| DENOMINAZIONE\_COMUNE |  |
| SIGLA\_PROVINCIA |  |
| DENOMINAZIONE\_PROVINCIA |  |

### CONTATORI

Contatori utilizzati per la codificazione di richieste e reclami; sono incrementati dall’applicazione a ogni inserimento.

| **Campo** | **Descrizione** |
| - | - |
| CONTATORE | Richieste o Reclami |
| VALORE | Ultimo valore utilizzato per la generazione del codice |

### QUESTIONARI\_COMPILAZIONE

Dati di compilazione dei questionari.

| **Campo** | **Descrizione** |
| - | - |
| ID | Identificativo del record |
| TIPO\_QUESTIONARIO\_CODICE |  |
| SORGENTE |  |
| DATA\_ORA\_COMPLETAMENTO |  |

### QUESTIONARI\_DOMANDA

Dati di configurazione delle domande dei questionari.

| **Campo** | **Descrizione** |
| - | - |
| ID | Identificativo del record |
| TIPO\_QUESTIONARIO\_CODICE |  |
| ORDINE |  |
| TIPO\_DOMANDA |  |
| TESTO |  |
| OBBLIGATORIA |  |

### QUESTIONARI\_OPZIONE\_RISPOSTA

Dati di configurazione delle possibili risposte per le domande con selezione.

| **Campo** | **Descrizione** |
| - | - |
| ID | Identificativo del record |
| DOMANDA\_ID | Identificativo della domanda |
| POSIZIONE |  |
| CODICE\_NUMERICO |  |
| TESTO |  |

### QUESTIONARI\_RISPOSTA\_COMPILATA

Valori delle risposte compilate.

| **Campo** | **Descrizione** |
| - | - |
| ID | Identificativo del record |
| COMPILAZIONE\_ID |  |
| DOMANDA\_ID | Identificativo della domanda |
| OPZIONE\_RISPOSTA\_ID | Identificativo dell’opzione di risposta selezionata |
| CODICE\_NUMERICO\_SNAPSHOT |  |
| TESTO\_RISPOSTA |  |

### QUESTIONARI\_SORGENTE

Sorgenti dei questionari.

| **Campo** | **Descrizione** |
| - | - |
| SORGENTE | Identificativo del record |
| ABILITATA |  |

### QUESTIONARI\_TIPO

Configurazione dei tipi di questionari da indicare nell’URL per la compilazione.

| **Campo** | **Descrizione** |
| - | - |
| CODICE | Identificativo del record |
| NOME |  |
| DESCRIZIONE |  |

### QUESTIONARI\_TOKEN

Token per i questionari che consentono una sola compilazione.

| **Campo** | **Descrizione** |
| - | - |
| TOKEN | Token generato dall’applicazione per l’uso una tantum |
| TIPO\_QUESTIONARIO\_CODICE |  |
| SORGENTE |  |
| DATA\_UTILIZZO |  |
| EMAIL |  |

### REGISTRO\_ATTIVITA

Log dei principali eventi di gestione di una pratica.

| **Campo** | **Descrizione** |
| - | - |
| ID\_LOG | Identificativo del record |
| TIPO\_SEGNALAZIONE |  |
| ID\_SEGNALAZIONE |  |
| DATA\_ORA\_EVENTO |  |
| OPERATORE |  |
| STATO\_PRATICA |  |
| DETTAGLI |  |

### TIPI\_AUTORE\_RICHIESTA

Tipologie di autore di una richiesta selezionabili all’inserimento.

| **Campo** | **Descrizione** |
| - | - |
| ID\_TIPO\_AUTORE\_RICHIESTA | Identificativo del record |
| DESCRIZIONE\_TIPO\_AUTORE\_RICHIESTA |  |
| ORDINE |  |

### TIPI\_LUOGO

Tipologie di luogo di una richiesta selezionabili all’inserimento.

| **Campo** | **Descrizione** |
| - | - |
| ID\_TIPO\_LUOGO | Identificativo del record |
| DESCRIZIONE\_TIPO\_LUOGO |  |
| ORDINE |  |

### TIPI\_RICHIESTA

Tipologie di richieste inseribili.

| **Campo** | **Descrizione** |
| - | - |
| ID\_TIPO\_RICHIESTA | Identificativo del record |
| DESCRIZIONE\_TIPO\_RICHIESTA |  |
| DETTAGLIO |  |
| ORDINE |  |

### TIPI\_SORGENTE

Tipologie di sorgenti dei questionari.

| **Campo** | **Descrizione** |
| - | - |
| TIPO\_SORGENTE | Identificativo del record |
| ORDINE |  |

### TIPI\_STRUTTURA

Tipologie di struttura di un reclamo selezionabili all’inserimento.

| **Campo** | **Descrizione** |
| - | - |
| CODICE\_STRUTTURA | Identificativo del record |
| DESCRIZIONE\_STRUTTURA |  |
| ORDINE |  |

### TIPI\_TEMATICA

Tipologie di tematica di una richiesta selezionabili all’inserimento.

| **Campo** | **Descrizione** |
| - | - |
| ID\_TIPO\_TEMATICA | Identificativo del record |
| DESCRIZIONE\_TIPO\_TEMATICA |  |
| ORDINE |  |








## 8. Dati da servizi esterni

L’applicazione GestioneURP utilizza Web service di anagrafiche esterne per verificare gli utenti abilitati all’accesso con relativi profili e le strutture aziendali utilizzate nella gestione di una richiesta.

Sono chiamati i seguenti Web service:

- Logon: autenticazione di un utente

- GetAppUserProfiles: profili dell’utente per l’applicazione

- GetApplicationUsers: utenti autorizzati per l’applicazione

- GetAllStrutture: elenco delle strutture aziendali

Di seguito i dati di richiesta in http POST, relativa risposta ed esempio di risposta.


### Logon

Dato un utente, ottenuto dal browser tramite autenticazione integrata Windows (IWA), verifica il login ritornando l’identificativo utente o un numero negativo in caso di accesso fallito.

#### *Richiesta*

POST /gsows/gso.asmx/Logon HTTP/1.1

Host: localhost

Content-Type: application/x-www-form-urlencoded

Content-Length: length

sUserName=string&sPassword=string&sApplication=string&bTrusted=string

#### Risposta

HTTP/1.1 200 OK

Content-Type: text/xml; charset=utf-8

Content-Length: length

\<?xml version="1.0" encoding="utf-8"?\>

\<int xmlns="http://tempuri.org/"\>int\</int\>

##### Esempio risposta:

\<?xml version="1.0" encoding="utf-8"?\>

\<int xmlns="http://tempuri.org/"\>4242\</int\>


### GetAppUserProfiles

Dato un identificativo utente e il nome dell’applicazione, ne ritorna i profili di accesso.

#### Richiesta:

POST /gsows/gso.asmx/GetAppUserProfiles HTTP/1.1

Host: localhost

Content-Type: application/x-www-form-urlencoded

Content-Length: length

nUserId=string&sApplication=string

#### Risposta:

HTTP/1.1 200 OK

Content-Type: text/xml; charset=utf-8

Content-Length: length

\<?xml version="1.0" encoding="utf-8"?\>

\<DataSet xmlns="http://tempuri.org/"\>

  \<schema xmlns="http://www.w3.org/2001/XMLSchema"\>schema\</schema\>xml\</DataSet\>

#### *Esempio risposta:*

\<?xml version="1.0" encoding="utf-8"?\>

\<DataSet xmlns="http://tempuri.org/"\>

  \<xs:schema id="NewDataSet" xmlns="" xmlns:xs="http://www.w3.org/2001/XMLSchema" xmlns:msdata="urn:schemas-microsoft-com:xml-msdata" xmlns:msprop="urn:schemas-microsoft-com:xml-msprop"\>

    \<xs:element name="NewDataSet" msdata:IsDataSet="true" msdata:UseCurrentLocale="true"\>

      \<xs:complexType\>

        \<xs:choice minOccurs="0" maxOccurs="unbounded"\>

          \<xs:element name="Table1" msprop:SqlHash="489910736" msprop:BaseTable.0="GSOV\_GET\_APP\_USER\_PROF"\>

            \<xs:complexType\>

              \<xs:sequence\>

                \<xs:element name="ID\_PROFILO\_UTENTE\_APP" msprop:BaseColumn="ID\_PROFILO\_UTENTE\_APP" msprop:OraDbType="113" type="xs:decimal" minOccurs="0" /\>

                \<xs:element name="DESCRIZIONE" msprop:BaseColumn="DESCRIZIONE" msprop:OraDbType="126" type="xs:string" minOccurs="0" /\>

                \<xs:element name="PROFILO" msprop:BaseColumn="PROFILO" msprop:OraDbType="109" type="xs:string" minOccurs="0" /\>

              \</xs:sequence\>

            \</xs:complexType\>

          \</xs:element\>

        \</xs:choice\>

      \</xs:complexType\>

    \</xs:element\>

  \</xs:schema\>

  \<diffgr:diffgram xmlns:msdata="urn:schemas-microsoft-com:xml-msdata" xmlns:diffgr="urn:schemas-microsoft-com:xml-diffgram-v1"\>

    \<NewDataSet xmlns=""\>

      \<Table1 diffgr:id="Table11" msdata:rowOrder="0"\>

        \<ID\_PROFILO\_UTENTE\_APP\>7373\</ID\_PROFILO\_UTENTE\_APP\>

        \<DESCRIZIONE\>Generazione questionari URP URP\</DESCRIZIONE\>

        \<PROFILO\>&lt;?xml version="1.0" encoding="ISO-8859-1" ?&gt;

&lt;urp\_gestione&gt;

    &lt;profilo nome="GenerazioneUrlQuestionari URP"&gt;

        &lt;funz valore="Accesso" abilit="true"/&gt;

        &lt;funz valore="Amministratore" abilit="false"/&gt;

        &lt;funz valore="Gestore" abilit="false"/&gt;

	&lt;funz valore="GenerazioneUrlQuestionari" abilit="true"/&gt;

    &lt;/profilo&gt;

&lt;/urp\_gestione&gt;\</PROFILO\>

      \</Table1\>

      \<Table1 diffgr:id="Table12" msdata:rowOrder="1"\>

        \<ID\_PROFILO\_UTENTE\_APP\>7474\</ID\_PROFILO\_UTENTE\_APP\>

        \<DESCRIZIONE\>Generazione questionari URP URP\</DESCRIZIONE\>

        \<PROFILO\>&lt;?xml version="1.0" encoding="ISO-8859-1" ?&gt;

&lt;urp\_gestione&gt;

    &lt;profilo nome="GenerazioneUrlQuestionari URP"&gt;

        &lt;funz valore="Accesso" abilit="true"/&gt;

        &lt;funz valore="Amministratore" abilit="false"/&gt;

        &lt;funz valore="Gestore" abilit="false"/&gt;

	&lt;funz valore="GenerazioneUrlQuestionari" abilit="true"/&gt;

    &lt;/profilo&gt;

&lt;/urp\_gestione&gt;\</PROFILO\>

      \</Table1\>

\</NewDataSet\>

  \</diffgr:diffgram\>

\</DataSet\>


### GetApplicationUsers

Dato il nome dell’applicazione, ne ritorna gli utenti e relativi profili di accesso.

#### Richiesta:

POST /gsows/gso.asmx/GetApplicationUsers HTTP/1.1

Host: localhost

Content-Type: application/x-www-form-urlencoded

Content-Length: length

sApplication=string

#### Risposta:

HTTP/1.1 200 OK

Content-Type: text/xml; charset=utf-8

Content-Length: length

\<?xml version="1.0" encoding="utf-8"?\>

\<DataSet xmlns="http://tempuri.org/"\>

  \<schema xmlns="http://www.w3.org/2001/XMLSchema"\>schema\</schema\>xml\</DataSet\>

#### *Esempio risposta:*

\<?xml version="1.0" encoding="utf-8"?\>

\<DataSet xmlns="http://tempuri.org/"\>

  \<xs:schema id="NewDataSet" xmlns="" xmlns:xs="http://www.w3.org/2001/XMLSchema" xmlns:msdata="urn:schemas-microsoft-com:xml-msdata" xmlns:msprop="urn:schemas-microsoft-com:xml-msprop"\>

    \<xs:element name="NewDataSet" msdata:IsDataSet="true" msdata:UseCurrentLocale="true"\>

      \<xs:complexType\>

        \<xs:choice minOccurs="0" maxOccurs="unbounded"\>

          \<xs:element name="Table1" msprop:SqlHash="890921298" msprop:BaseTable.0="GSOV\_GET\_APP\_USER\_PROF" msprop:BaseTable.1="GSO\_UTENTI"\>

            \<xs:complexType\>

              \<xs:sequence\>

                \<xs:element name="ID\_UTENTE" msprop:BaseColumn="ID\_UTENTE" msprop:OraDbType="113" type="xs:decimal" minOccurs="0" /\>

                \<xs:element name="NOME" msprop:BaseColumn="NOME" msprop:OraDbType="126" type="xs:string" minOccurs="0" /\>

                \<xs:element name="COGNOME" msprop:BaseColumn="COGNOME" msprop:OraDbType="126" type="xs:string" minOccurs="0" /\>

                \<xs:element name="DESCRIZIONE\_PROFILO" msprop:BaseColumn="DESCRIZIONE" msprop:OraDbType="126" type="xs:string" minOccurs="0" /\>

              \</xs:sequence\>

            \</xs:complexType\>

          \</xs:element\>

        \</xs:choice\>

      \</xs:complexType\>

    \</xs:element\>

  \</xs:schema\>

  \<diffgr:diffgram xmlns:msdata="urn:schemas-microsoft-com:xml-msdata" xmlns:diffgr="urn:schemas-microsoft-com:xml-diffgram-v1"\>

    \<NewDataSet xmlns=""\>

      \<Table1 diffgr:id="Table11" msdata:rowOrder="0"\>

        \<ID\_UTENTE\>4242\</ID\_UTENTE\>

        \<NOME\>Mario\</NOME\>

        \<COGNOME\>Rossi\</COGNOME\>

        \<DESCRIZIONE\_PROFILO\>Generazione questionari URP URP\</DESCRIZIONE\_PROFILO\>

      \</Table1\>

    \</NewDataSet\>

  \</diffgr:diffgram\>

\</DataSet\>


### GetAllStrutture

Nessun parametro. Ritorna l’elenco delle strutture aziendali.

#### *Richiesta:*

POST /gsows/gso.asmx/GetAllStrutture HTTP/1.1

Host: localhost

Content-Type: application/x-www-form-urlencoded

Content-Length: length

#### Risposta:

HTTP/1.1 200 OK

Content-Type: text/xml; charset=utf-8

Content-Length: length

\<?xml version="1.0" encoding="utf-8"?\>

\<DataSet xmlns="http://tempuri.org/"\>

  \<schema xmlns="http://www.w3.org/2001/XMLSchema"\>schema\</schema\>xml\</DataSet\>

#### *Esempio risposta:*

\<?xml version="1.0" encoding="utf-8"?\>

\<DataSet xmlns="http://tempuri.org/"\>

  \<xs:schema id="NewDataSet" xmlns="" xmlns:xs="http://www.w3.org/2001/XMLSchema" xmlns:msdata="urn:schemas-microsoft-com:xml-msdata" xmlns:msprop="urn:schemas-microsoft-com:xml-msprop"\>

    \<xs:element name="NewDataSet" msdata:IsDataSet="true" msdata:UseCurrentLocale="true"\>

      \<xs:complexType\>

        \<xs:choice minOccurs="0" maxOccurs="unbounded"\>

          \<xs:element name="Table1" msprop:SqlHash="-332345214" msprop:BaseTable.0="GSOV\_STRUTTURE"\>

            \<xs:complexType\>

              \<xs:sequence\>

                \<xs:element name="ID\_STRUTTURA" msprop:BaseColumn="ID\_STRUTTURA" msprop:OraDbType="113" type="xs:decimal" minOccurs="0" /\>

                \<xs:element name="DESCRIZIONE\_STRUTTURA" msprop:BaseColumn="DESCRIZIONE\_STRUTTURA" msprop:OraDbType="126" type="xs:string" minOccurs="0" /\>

                \<xs:element name="SEDE" msprop:BaseColumn="SEDE" msprop:OraDbType="126" type="xs:string" minOccurs="0" /\>

                \<xs:element name="COD\_STRUTTURA\_ORGANIZZATIVA" msprop:BaseColumn="COD\_STRUTTURA\_ORGANIZZATIVA" msprop:OraDbType="126" type="xs:string" minOccurs="0" /\>

                \<xs:element name="ID\_STRUTTURA\_ORG\_PADRE" msprop:BaseColumn="ID\_STRUTTURA\_ORG\_PADRE" msprop:OraDbType="113" type="xs:decimal" minOccurs="0" /\>

              \</xs:sequence\>

            \</xs:complexType\>

          \</xs:element\>

        \</xs:choice\>

      \</xs:complexType\>

    \</xs:element\>

  \</xs:schema\>

  \<diffgr:diffgram xmlns:msdata="urn:schemas-microsoft-com:xml-msdata" xmlns:diffgr="urn:schemas-microsoft-com:xml-diffgram-v1"\>

    \<NewDataSet xmlns=""\>

      \<Table1 diffgr:id="Table11" msdata:rowOrder="0"\>

        \<ID\_STRUTTURA\>1\</ID\_STRUTTURA\>

        \<DESCRIZIONE\_STRUTTURA\>Struttura principale\</DESCRIZIONE\_STRUTTURA\>

        \<SEDE\>RM\</SEDE\>

        \<COD\_STRUTTURA\_ORGANIZZATIVA\>ABC\</COD\_STRUTTURA\_ORGANIZZATIVA\>

        \<ID\_STRUTTURA\_ORG\_PADRE\>0\</ID\_STRUTTURA\_ORG\_PADRE\>

      \</Table1\>

      \<Table1 diffgr:id="Table12" msdata:rowOrder="1"\>

        \<ID\_STRUTTURA\>2\</ID\_STRUTTURA\>

        \<DESCRIZIONE\_STRUTTURA\>Struttura secondaria\</DESCRIZIONE\_STRUTTURA\>

        \<SEDE\>RM\</SEDE\>

        \<COD\_STRUTTURA\_ORGANIZZATIVA\>DEF\</COD\_STRUTTURA\_ORGANIZZATIVA\>

        \<ID\_STRUTTURA\_ORG\_PADRE\>1\</ID\_STRUTTURA\_ORG\_PADRE\>

      \</Table1\>

    \</NewDataSet\>

  \</diffgr:diffgram\>

\</DataSet\>







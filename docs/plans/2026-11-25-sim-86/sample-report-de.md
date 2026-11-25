# Bericht über einen Simulationslauf

Erstellt aus dem Laufprotokoll `clinic-run.json` mit SimOpt.McpServer 1.0.0+ff2c874 (`report_run`). Die Zahlen sind die protokollierten, auf vier signifikante Stellen.

## Herkunft

| | |
|---|---|
| Protokoll | `clinic-run.json`, simopt-run/1 |
| Hashwert des Protokolls (SHA-256) | `349f929430a72fa19cb24d74b443baa664c1648114813d0bf87ce5e6264811b9` |
| Unversehrtheit des Protokolls | Unverändert: das Protokoll ergibt weiterhin seinen record_sha256. |
| Modell | clinic, Version 1 |
| Hashwert des Modells (SHA-256) | `29fc47074d8f13fafe1b369b4d7c5d6aad028e5a739b64015d3e0c4931eb94c0` |
| Unversehrtheit des Modells | Unverändert: das Modell im Protokoll ergibt den model_sha256, den das Ergebnis nennt. |
| Werkzeugaufruf | `run_experiment` |
| Protokolliert mit | SimOpt.McpServer 1.0.0+ff2c874 |

## Laufeinstellungen

| | |
|---|---|
| Laufzeit | 540 |
| Einschwingphase | 54 |
| Replikationen | 5 |
| Basis-Startwert | 7 |
| Konfidenzniveau | 95 % |
| Abgefragte Kennzahlen | alle |

Replikation r läuft mit einem aus dem Basis-Startwert abgeleiteten Startwert (ReplicationRunner.SeedFor); derselbe Basis-Startwert ergibt auf jedem Rechner dieselben Replikationen.

## Szenario: baseline

| Kennzahl | Mittelwert | 95-%-Intervall |
|---|---|---|
| wait_exam | 1,18 | [0,3678; 1,993] |
| los | 31,01 | [30,15; 31,88] |

## Szenario: flu

Aus dem Modell gebildet durch: `set arrivals.arrival.mean = 4`

| Kennzahl | Mittelwert | 95-%-Intervall |
|---|---|---|
| wait_exam | 4,331 | [1,762; 6,9] |
| los | 34,49 | [31,85; 37,12] |

| Kennzahl | Unterschied zur Basis | 95-%-Intervall | Signifikant |
|---|---|---|---|
| wait_exam | 3,151 | [1,251; 5,05] | ja |
| los | 3,475 | [1,348; 5,603] | ja |

## Was der Lauf angenommen hat

Werte, die der Lauf ohne Angabe verwendet hat, wie sein Ergebnis sie aufführt (UN-035):

| Wert | Angenommen als |
|---|---|
| warmup | ein Zehntel der Laufzeit: 54 |
| seed | wie im Modell: 7 |
| exam_q.capacity | unbegrenzt |
| exam_q.discipline | fifo |

## Was das Modell nicht abbildet

Die Grenzen von Schema v1, wie mit dem Lauf protokolliert (UN-035):

- Einheiten wechseln ohne Zeitverzug zwischen verbundenen Knoten; Wege, Transport und Handhabung brauchen nur dort Zeit, wo ein Verzögerungsknoten oder ein Verzögerungsschritt steht.
- Stationen fallen nie aus, und ihre Bedienstellen arbeiten ohne Schichten, Pausen oder Rüstvorgänge.
- Ankünfte folgen über den ganzen Lauf einer einzigen Verteilung: kein Tagesgang und keine Spitzenzeiten.
- Alle Einheiten sind gleich: keine Typen, Merkmale oder Prioritäten; eine Warteschlange bedient in Ankunftsreihenfolge oder in umgekehrter Reihenfolge, und eine Verzweigung berücksichtigt die Einheit nie.
- Die Bedienstellen einer Station sind gleich und bedienen jeweils eine Einheit, und eine Station teilt nichts mit einer anderen; was Tätigkeiten teilen (Personal, Plätze), ist eine Ressource, die Prozessschritte belegen, eine Einheit je Schritt.
- Ein Prozess hat keine Schleifen oder Bedingungen, und eine Einheit hält Ressourcen nur innerhalb eines Prozessknotens; beim Verlassen gibt sie alles frei, was sie noch hält.
- Eine Einheit wartet in ihrer Warteschlange, bis sie bedient wird, und geht nie vorzeitig, außer an einem Belegungs- oder Entnahmeschritt mit Geduld; eine Einheit, die eine volle Warteschlange abweist, geht verloren und wird gezählt.
- Jede Replikation beginnt leer und untätig; nur die Einschwingphase hält diesen Anfang aus den Kennzahlen heraus.

## Wie man den Lauf wiederholt

1. Mit dem SimOpt-MCP-Server: `reproduce_run` mit dem Pfad dieses Protokolls aufrufen. Es führt den protokollierten Aufruf am protokollierten Modell aus, allein aus dem Protokoll, und sagt, ob jede Zahl gleich herauskam.
2. In Python: `simopt.Server().reproduce_run("clinic-run.json")`.
3. Das Modell allein: das Objekt `model` im Protokoll ist das Schema-v1-Dokument; als Datei gespeichert, öffnet es `load_model`, und sein SHA-256 ist der oben genannte Hashwert des Modells.

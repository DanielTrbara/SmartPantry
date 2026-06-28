# 🍳 Smart Pantry

Eine intuitive Webanwendung zur Verwaltung des eigenen Vorratsschranks, zur dynamischen Rezeptplanung und zur Reduzierung von Lebensmittelverschwendung. Entwickelt als Studienprojekt für das Modul **Datenbanken 2** an der **Hochschule Esslingen**.

## 🚀 Features

- **Schlankes User-Management:** Einfacher, session-basierter Login direkt über die SQL-Datenbank (ohne externen Overhead).
- **Intelligente Vorratskammer:** Übersicht über vorhandene Lebensmittel inklusive Mindesthaltbarkeitsdatum (MHD).
- **Dynamisches Kochen:** Rezepte können eingesehen, modifiziert und gekocht werden. Beim Kochvorgang können Zutatenmengen flexibel angepasst oder durch Alternativen ersetzt werden – die Datenbank zieht exakt die real verbrauchten Mengen ab.
- **Datenbank-zentrierte Logik:** Die Kernlogik (MHD-Warnungen, dynamischer Bestandsabzug, intelligente Einkaufsunterstützung) wird direkt über den MS-SQL Server via Trigger, Stored Procedures und Functions abgewickelt.

## 🛠️ Tech Stack

- **Frontend/Backend:** Web-native Applikation (Vue.js)
- **Datenbank:** Microsoft SQL Server

## Setup project

- If you want to use Bootstrap
  -> create LibMan: dotnet libman init --provider cdnjs
  -> install bootstrap: dotnet libman install bootstrap@5.3.0 --destination wwwroot/lib/bootstrap
- Update Connection String to connect to your Database via Secret

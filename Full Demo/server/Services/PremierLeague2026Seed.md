# 2026 Premier League seed data

Both demo servers use `PremierLeague2026Seed.cs` for the 2026 season. The data
contains the 110 matches played on Nights 01–16 and the three playoff matches
on 28 May 2026, including Luke Littler's 11–10 final win over Luke Humphries.
Each match includes the published scores, three-dart averages, 180 counts and
highest checkouts. Dates represent the event date at midnight UTC.

Sources, retrieved on 3 October 2026:

- [PDC Night 1 tournament hub](https://www.pdc.tv/tournament-hub/10702).
  The subsequent nights use hub IDs 10703–10717.
- [PDC Finals tournament hub](https://www.pdc.tv/tournament-hub/10718).
- The PDC hubs' fixture timelines supply the statistics shown in each match's
  Stats tab, via `https://fixtures.darts.web.gc.pdcservices.co.uk/v2/{fixtureID}/timeline`.
- [Mastercaller results](https://mastercaller.com/tournaments/premier-league/2026/results)
  provide a secondary check of the scores and averages.

Michael van Gerwen withdrew on Night 03, giving Luke Littler a walkover.
Gian van Veen withdrew on Night 07, giving Van Gerwen a walkover. These two
unplayed fixtures are excluded because the match model has no walkover status;
storing a fabricated leg score would distort the match statistics.

The database seeder adds missing 2026 participants and results to existing
databases. Repeated startup preserves existing records and avoids duplicates,
including records with the players stored in the opposite order. Historical
2024/2025 seeds remain unchanged. The client defaults to 2026 and retains the
earlier seasons in its selector.

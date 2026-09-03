NEXT STEP — build the 61 → 13 Pāli corpus

Files to add

PROJECT_THERAVADA/scripts/build_pali_corpus.py

.github/workflows/build-theravada-corpus.yml

The workflow is deliberately read-only. It does not edit or commit the source repository.
It creates the 13 consolidated files inside the GitHub Actions runner and publishes them
as a downloadable Actions artifact for inspection.

Run

GitHub → Actions → Build Theravada Pali Corpus → Run workflow.

After the run completes:

Open the completed workflow run.

Confirm the Build and validate 61 -> 13 corpus step is green.

Read the printed BUILD_REPORT.md.

Download the artifact theravada-pali-corpus-13.

Do not upload the 13 TXT files into ChatGPT sources until the report says:

61/61 mapped sources built

13/13 output files created

duplicate assignments = 0

missing files = 0

Send the build report or the artifact back to ChatGPT for a second audit.

Important source decision

Use romn/*.xml as the working source for the 13 Roman-Pāli research files.

root text files/*.txt are Devanāgarī source-format texts, not Roman-Pāli duplicates.
They remain valuable for provenance/control but should not be merged into the Roman-Pāli
13-file corpus.

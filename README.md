# Tipitaka.org

Source files for Tipitaka.org website.

##The HTML Source files
---------------------
These are located in the tipitaka.org folder.

##Code Converters
-----------------

The also contains various Python and C# code snippets to perform different types of script conversions needed for the site.

Primary content of the Tipitaka sourcefiles are captured in Devanagari script and stored in ==deva_master== folder. Any updates needed in the tipitaka text (corrections, typo errors, etc.) are carried out in Devanagari script. 

A C# conversion script is then run to generate the transliterated text for all othe supported scripts (see code/converters).

For addition of new content, e.g. in the Anna section, an additional processing is required, i.e. new text files are created in Markdown MD fommat. Then a Python script is then run to covnert the markdown file into an xml file (see the Python script in /code/md2xmlconverter).  

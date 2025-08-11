Code/converters/TreeC#JsonConverter


Purpose: Whenever the Tipitaka website tree structure is edited from the file tree.json, Deva script is used as the source language. Once the changes have been completed and verified we need to generate the new tree from the Deva tree by running the C# script program.cs 

How to generate other json trees from Deva tree
===============================================

1. Copy and paste the tree.json file from tipitaka.org/deva into the current folder.
2. Assuming C# has been installed, run the command dotnet run in command line. Note the input file tree.json is hardcoded in program.cs as well as the output folder TreeConverted. Modify the output folder as required or keep it as it is.
3. Once the program.cs has been run, it will generate json trees of each of the supported scripts (except for Tamil) in the output folder TreeConverted. The output files will be named tree_<4-letter_script>.json.
4. For all other scripts except Roman (i.e. tree_latn.json) copy each of these files into their respective script folder, e.g. tipitaka.org/cyrl for tree_cyrl.json and rename it as tree.json.
5. There is one more additional step required for the Roman script in that the converted script are not capitalised correctly, hence an additional script needs to be run.
6. Navigate to the TreeConverted folder and run the python script latn_capitaliser.py. This will generate an output file 'tree_latn_cap.json'. 
7. Copy the tree_latn_cap.json to tipitaka.org/romn and rename it as tree.json

This completes the new tree generation process.



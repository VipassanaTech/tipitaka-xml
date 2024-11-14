# The Deva2Latn script is not capitalising the first letter of each word correctly.
# This Python script corrects this problem.
# Dev by Leonard Aye (leonardaye@gmail.com)

import re

# Get the input and output file paths from the user
input_file_path = "tree_latn.json"
output_file_path = "tree_latn_cap.json"

# Define a regex pattern to match the content following "text": and inside quotes
pattern = re.compile(r'(\s*"text":\s*")([^"]+)"')

# Read the input file, process each line, and capitalize each word in "text" lines
new_lines = []
with open(input_file_path, 'r', encoding="utf-16") as infile:
    for line in infile:
        # Check if the line contains "text" with leading spaces
        match = pattern.search(line)
        if match:
            # Capture leading spaces, "text": prefix, and the actual text content
            leading_spaces_and_prefix = match.group(1)
            text_content = match.group(2)

            # Capitalize each word in the text content, including those within parentheses
            capitalized_text = re.sub(r'\b\w+\b', lambda m: m.group(0).capitalize(), text_content)
            
            # Reconstruct the line with capitalized text and preserve leading spaces
            line = f'{leading_spaces_and_prefix}{capitalized_text}"\n'

        # Add the line (modified or unmodified) to the list of new lines
        new_lines.append(line)

# Write the modified lines to the output file
with open(output_file_path, 'w', encoding="utf-16") as outfile:
    outfile.writelines(new_lines)

print("Processing complete. Check the output file:", output_file_path)

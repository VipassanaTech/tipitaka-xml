# This script reads a markdown file (.md) from a user-specified 
# folder and filename, then converts it into a structured XML 
# format following specified rules:
#
# Conversion Rules:
# - Lines enclosed by single '#' at start and end: 
#       # text # 
#   Converted to:
#       <p rend="chapter">text</p>
#
# - Lines enclosed by triple '#' at start and end: 
#       ### text ### 
#   Converted to:
#       <p rend="subhead">text</p>
#
# - Normal lines ending with two spaces ("  "):
#       text  (two trailing spaces)
#   Converted to:
#       <p rend="bodytext">text</p>
#
# - A line containing only <br>:
#       <br>
#   Converted to an empty body text paragraph:
#       <p rend="bodytext"></p>
#
# - Gatha Lines: Lines beginning with '> >':
#       > > gatha line text
#   These lines are grouped together until a line that does not start with '> >' is encountered.
#   The first line of a gatha group is <p rend="gatha1">...</p>, the second is <p rend="gatha2">...</p>, 
#   and so forth until the last line is <p rend="gathalast">...</p>.
#   If there is only one gatha line in the group, it is directly assigned "gathalast".
#
# The code:
# 1. Prompts the user for the folder path and the markdown filename.
# 2. Attempts to open and read the markdown file.
# 3. Processes each line according to the above rules.
# 4. Outputs the result to an XML file with the same base name as the markdown file.
# 5. Uses try-except blocks and print statements to guide troubleshooting.
#
# Note:
# - Ensure that the given folder and file are correct and that you have permission to read/write files.
# - If any error occurs (like file not found), the code will handle it gracefully and print error messages.
###############################################################


import os
import re
# Prompt for folder and file name
folder = ""
filename = input("Enter md file: ")

input_path = os.path.join(folder, filename)
output_filename = filename.replace('.md', '.xml')
output_path = os.path.join(folder, output_filename)

# Define the XML Header and Footer
xml_header = '''<?xml version="1.0" encoding="UTF-16"?>
<?xml-stylesheet type="text/xsl" href="tipitaka-deva.xsl"?>
<TEI.2>
<teiHeader></teiHeader>
<text>
<front></front>
<body>
'''

xml_footer = '''
</body>
</text>
</TEI.2>
'''

import re

def manage_bold(text):
    """
    This function takes a string that may contain multiple occurrences of bold markup 
    using the markdown syntax '**bold text**'. It will convert these segments into
    an XML format that represents bold text as:
        <hi rend="bold">bold text</hi>

    For example:
        Input: "This is **bold** text and **another bold** segment."
        Output: "This is <hi rend=\"bold\">bold</hi> text and <hi rend=\"bold\">another bold</hi> segment."

    If there are no bold markers in the text, the original string will be returned as is.

    If there are unmatched or malformed bold indicators, they will remain unchanged 
    since the regex only replaces well-formed pairs of '**'.
    """

    # The regex pattern:
    # \*\*  matches literal '**'
    # (.*?) is a non-greedy match for any characters inside the bold markers
    # \*\*  matches the closing '**'
    # Using a raw string to avoid escaping issues, and re.DOTALL is not needed here since we don't expect multiline within bold.
    pattern = re.compile(r'\*\*(.*?)\*\*')
    print('pattern: ',pattern)

    # For each match, we replace with the XML bold tag
    # The matched text is accessed with \1 or using a lambda function's match object group(1)
    def replacer(match):
        bold_text = match.group(1)  # text inside ** **
        return f'<hi rend="bold">{bold_text}</hi>'

    # Use sub to replace all occurrences of **...** with <hi rend="bold">...</hi>
    result = pattern.sub(replacer, text)
    return result

# Example usage:
# text_input = "This is **bold** text and **another bold** segment."
# print(manage_bold(text_input))
# Output: This is <hi rend="bold">bold</hi> text and <hi rend="bold">another bold</hi> segment.


## This works fine. gatha1, 2, 3...... but need to change to gatha1, and gatha-last only. 
# def flush_gatha_block(gatha_lines, output_file):
#     """Write out a collected group of gatha lines to the XML file."""
#     if not gatha_lines:
#         return
    
#     # If only one gatha line, label it 'gathalast'
#     if len(gatha_lines) == 1:
#         output_file.write(f'<p rend="gathalast">{gatha_lines[0]}</p>\n')
#         print("[DEBUG] Wrote single gatha line as gathalast.")
#     else:
#         # Multiple gatha lines
#         for i, gline in enumerate(gatha_lines):
#             if i == 0:
#                 rend = "gatha1"
#             elif i == len(gatha_lines) - 1:
#                 rend = "gathalast"
#             else:
#                 rend = f"gatha{i+1}"
#             output_file.write(f'<p rend="{rend}">{gline}</p>\n')
#         print(f"[DEBUG] Wrote {len(gatha_lines)} gatha lines.")
#     gatha_lines.clear()


def flush_gatha_block(gatha_lines, output_file):
    """Write out a collected group of gatha lines to the XML file."""
    if not gatha_lines:
        return
    
    # If only one gatha line, label it 'gathalast'
    if len(gatha_lines) == 1:
        output_file.write(f'<p rend="gathalast">{gatha_lines[0]}</p>\n')
        print("[DEBUG] Wrote single gatha line as gathalast.")
    else:
        # Multiple gatha lines
        for i, gline in enumerate(gatha_lines):
            if i == 0:
                rend = "gatha1"
            elif i == len(gatha_lines) - 1:
                rend = "gathalast"
            else:
                # For all intermediate lines, also use gatha1
                rend = "gatha1"
            output_file.write(f'<p rend="{rend}">{gline}</p>\n')
        #print(f"[DEBUG] Wrote {len(gatha_lines)} gatha lines.")
    gatha_lines.clear()


# Main conversion logic
try:
    # Check if file exists
    if not os.path.isfile(input_path):
        raise FileNotFoundError(f"File not found at: {input_path}")

    # Open input and output files
    with open(input_path, 'r', encoding='utf-8') as infile, open(output_path, 'w', encoding='utf-8') as outfile:
        print(f"[INFO] Reading from: {input_path}")
        print(f"[INFO] Writing to: {output_path}")

        # Write the XML header
        outfile.write(xml_header + "\n")
        print("[INFO] Wrote XML header.")

        gatha_buffer = []

        line_count = 0
        for line in infile:
            line_count += 1
            original_line = line  # Keep the original line for debugging if needed
            line = line.rstrip('\n')  # Remove newline character at end
            line=manage_bold(line)
            stripped = line.strip()
            # Check for gatha lines first
            if line.strip().startswith('>') and line.strip().endswith('>'):
                # We have a gatha line enclosed by '>' at the start and end.
                # Example: ">Hello world >"
                # Remove the first and last '>'
                gatha_text = line.strip()[1:-1].strip()
                gatha_buffer.append(gatha_text)
                #print(f"[DEBUG] Gatha line detected: {gatha_text}")
                outfile.write(f'<p rend="gatha1">{gatha_text}</p>\n')
                continue

              
             # Case A: ### text ###
            # Check triple-hash lines first
            if stripped.startswith('###') and stripped.endswith('###') and len(stripped) > 6:
                # Extract the text inside ###
                text = stripped[3:-3].strip()
                print("Managing ###")
                outfile.write(f'<p rend="subhead">{text}</p>\n')
                continue

            # Case B: # text #
            # Check single-hash lines after checking triple-hash
            if stripped.startswith('#') and stripped.endswith('#') and len(stripped) > 2:
                # Extract the text inside #
                text = stripped[1:-1].strip()
                outfile.write(f'<p rend="chapter">{text}</p>\n')
                continue
           

           

            # Case C: normal line with two trailing spaces
            # Note: we did not strip all trailing spaces initially, so we can check here.
            if line.endswith('  '):
                text = line.strip()
                outfile.write(f'<p rend="bodytext">{text}</p>\n')
                #print(f"[DEBUG] Line {line_count}: Bodytext line (ending with two spaces).")
                continue
                
              # Case D: <br> line
            if line.strip() == '<br>':
                outfile.write('<p rend="bodytext"></p>\n')
                #print(f"[DEBUG] Line {line_count}: <br> converted to empty bodytext.")
                continue

            stripped = line.strip()

            # If none of the above conditions met, treat it as a normal bodytext line.
            # If it's empty, just write an empty bodytext.
            if stripped:
                outfile.write(f'<p rend="bodytext">{stripped}</p>\n')
                #print(f"[DEBUG] Line {line_count}: Normal line treated as bodytext.")
            else:
                outfile.write('<p rend="bodytext"></p>\n')
                #print(f"[DEBUG] Line {line_count}: Empty line written as empty bodytext.")

        # If the file ended and we still have gatha lines, flush them.
        if gatha_buffer:
            flush_gatha_block(gatha_buffer, outfile)

        # Write the XML footer
        outfile.write(xml_footer)
        print("[INFO] Wrote XML footer.")
        print("[INFO] Conversion completed successfully.")

except FileNotFoundError as fnf_err:
    print("[ERROR] File not found:", fnf_err)
except PermissionError as p_err:
    print("[ERROR] Permission error. Check file permissions:", p_err)
except Exception as e:
    print("[ERROR] An unexpected error occurred:", e)

awk ' BEGIN { IFS=OFS="\t"; } ; { array[FNR]=$0; } ; ENDFILE { string=""; name=FILENAME; sub(/.txt/, "", name); gsub(/-/, ",", name); printf "%s", name OFS; for (i in array) { printf "%s", array[i] ; if (i < length(array)) printf "%s", "," }; print ""; split("", array) ; } ' *.txt
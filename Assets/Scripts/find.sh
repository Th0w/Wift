find . -type f | grep -ie '.cs$' | while read filename; do cnt=$(cat "$filename" | grep -i "SetActive" | wc -l);[[ $cnt -gt 0 ]] && cat "$filename" | less;done
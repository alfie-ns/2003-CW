#!/bin/bash
# Git Commit Importance Script
# - I made made this script over the summer holidays for my other git projects: alfie-ns
# - 'read -e' enables command-line editing (arrow keys)
# - '>&2' is used to redirect output to stderr thus only echoing the importance and custom message to the commit
set -e  # Exit immediately if a command exits with a non-zero status; this'll prevent an error being committed

# Function to print bold text
print_bold() {
    echo -e "\033[1m$1\033[0m"
}

# Function to get commit importance and custom message
get_commit_details() {
    local importance_text
    while true; do
        echo -n "Enter the importance (1-5): " >&2
        read -n1 importance # read only one character then auto-terminate
        echo >&2
        case $importance in
            1) importance_text="Trivial"; break;;
            2) importance_text="Minor"; break;;
            3) importance_text="Moderate"; break;;
            4) importance_text="Significant"; break;;
            5) importance_text="Milestone"; break;;
            *) echo "Invalid input. Please try again." >&2;;
        esac
    done
    echo -n "Enter a custom message for the commit: " >&2
    read -e custom_message
    echo >&2
    echo "${importance_text}: ${custom_message}"
}

# Function to selectively add files to staging
selective_add() {
    print_bold "\nUnstaged changes:"
    git status --porcelain | grep -E '^\s*[\?M]' | sed 's/^...//'
    # the above command lists all untracked and modified files;
    # untracked(?) and modified(M); the final sed command removes
    # the first three characters which are the status flags.
    while true; do
        echo -n "Enter file/directory to add, 'all' (or 'done' to finish): "
        read -e item
        if [ "$item" = "done" ]; then
            break
        elif [ "$item" = "all" ]; then
            git add .
            echo "Added all changes"
            break
        elif [ -e "$item" ]; then
            git add "$item"
            echo "Added: $item"
        else
            echo "File/directory not found. Please try again."
        fi
    done
}

# Function to review changes before pushing
review_changes() {
    print_bold "\nFiles included in this commit:"
    git show --stat --oneline HEAD # show commit stats
    
    while true; do
        print_bold "\nWould you like to push these changes? (y/n): "
        read -n1 answer # read only one character then auto-terminate
        echo
        case $answer in # if answer y|Y, push; if n|N, commit but don't push
            [Yy]) return 0;;
            [Nn]) 
                echo "Commit created but not pushed. You can:"
                echo "1. Push later with 'git push'"
                echo "2. Undo the commit with 'git reset HEAD~1'"
                return 1;;
            *) echo "Please answer y or n";;
        esac
    done
}

# Main Execution --------------------------------------------
# 1. Selectively add changes
selective_add

# 2. Get commit importance and custom message
print_bold "\nCommit importance:" >&2
echo "1. Trivial" >&2
echo "2. Minor" >&2
echo "3. Moderate" >&2
echo "4. Significant" >&2
echo -e "5. Milestone\n" >&2
commit_message=$(get_commit_details)

# 3. Commit changes
if git commit -m "$commit_message"; then
    echo "Changes committed successfully" >&2
    
    # 4. Review changes and confirm push
    if review_changes; then
        if git push origin main; then
            echo -e '\nLocal repo pushed to remote origin\n' >&2
            print_bold "Commit message: $commit_message" >&2
            exit 0 #success
        else
            echo "Error: Failed to push to remote..." >&2
            exit 1 #failure
        fi
    else
        exit 0 #success (commit made but push declined)
    fi
else
    echo "Error: Failed to commit changes..." >&2
    exit 1
fi
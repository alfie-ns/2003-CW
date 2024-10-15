#!/bin/bash

# Git Pull Script
# This script checks if the current branch is up-to-date with the remote,
# pulls changes if necessary, and guides the user through conflict resolution.

set -e  # Exit immediately if a command exits with a non-zero status

# Function to print bold text
print_bold() {
    echo -e "\033[1m$1\033[0m"
}

# Function to check if the current branch is up-to-date with the remote
check_up_to_date() {
    local branch_name
    branch_name=$(git rev-parse --abbrev-ref HEAD)
    
    if [ -z "$(git status --porcelain)" ]; then
        git fetch origin "$branch_name"
        if [ -z "$(git log HEAD..origin/$branch_name --oneline)" ]; then
            print_bold "Branch '$branch_name' is up-to-date with the remote."
            return 0
        else
            print_bold "Branch '$branch_name' is not up-to-date with the remote."
            return 1
        fi
    else
        print_bold "There are uncommitted changes. Please commit or stash them before proceeding."
        return 2
    fi
}

# Function to pull changes from the remote and handle conflicts
pull_changes() {
    local branch_name
    branch_name=$(git rev-parse --abbrev-ref HEAD)
    
    if [ -z "$(git status --porcelain)" ]; then
        print_bold "Pulling changes from the remote for branch '$branch_name'..."
        if git pull origin "$branch_name" --ff-only; then
            print_bold "Successfully pulled changes from the remote."
        else
            print_bold "Cannot fast-forward. Attempting to merge..."
            if git pull origin "$branch_name"; then
                print_bold "Merge successful."
            else
                handle_conflicts
            fi
        fi
    else
        print_bold "There are uncommitted changes. Please commit or stash them before pulling."
    fi
}

# Function to handle merge conflicts
handle_conflicts() {
    print_bold "Merge conflicts detected. Please resolve them manually."
    print_bold "Files with conflicts:"
    git diff --name-only --diff-filter=U
    
    print_bold "\nTo resolve conflicts:"
    print_bold "1. Open each conflicted file and look for conflict markers (<<<<<<, =======, >>>>>>>)."
    print_bold "2. Edit the files to resolve the conflicts."
    print_bold "3. Use 'git add <file>' to mark each resolved file."
    print_bold "4. Once all conflicts are resolved, run 'git commit' to complete the merge."
    print_bold "5. Push your changes with 'git push origin $branch_name'."
    
    read -p "Press Enter when you have resolved the conflicts, or type 'abort' to cancel the merge: " response
    if [[ $response == "abort" ]]; then
        git merge --abort
        print_bold "Merge aborted. Your repository is back to its previous state."
    else
        print_bold "Please complete the merge process manually."
    fi
}

# Main Execution --------------------------------------------
main() {
    print_bold "Checking Git repository status..."
    if check_up_to_date; then
        print_bold "No action needed. Your branch is up-to-date."
    else
        read -p "Do you want to pull changes from the remote? (y/n): " answer
        if [[ $answer =~ ^[Yy]$ ]]; then
            pull_changes
        else
            print_bold "Operation cancelled. No changes were pulled."
        fi
    fi
}

main

# End of script --------------------------------------------
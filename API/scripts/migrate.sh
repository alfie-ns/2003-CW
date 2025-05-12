#!/bin/bash

# Django Migration Script

# Function to print bold text
print_bold() {
    echo -e "\033[1m$1\033[0m"
}

# Function to make migrations
make_migrations() {
    print_bold "\nMaking migrations...\n"
    python manage.py makemigrations
}

# Function to migrate
migrate() {
    print_bold "\nMigrating...\n"
    python manage.py migrate
}

# Main function
main() {
    cd casino_simulator_api
    if make_migrations; then
        migrate
    else
        print_bold "\nMigrations failed\n"
        exit 1
    fi
    
}

# Execute main function
main
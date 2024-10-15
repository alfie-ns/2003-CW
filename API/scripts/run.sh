#!/bin/bash

# Django Server-run Script

cd casino_simulator_api

# Function to print bold text
print_bold() {
    echo -e "\033[1m$1\033[0m"
}

# Function to run the server
run_server() {
    print_bold "Running the server...\n"
    python manage.py runserver
    #python manage.py runserver 0.0.0.0:8000
}

# Function to migrate before running the server
migrate() {
    bash ../scripts/migrate.sh
}

# Main function ---------------------------------------------------------------
main() {
    if migrate; then
        print_bold "\nMigrations successful\n"
    else
        print_bold "\nMigrations failed\n"
        exit 1
    fi
    run_server
}

main

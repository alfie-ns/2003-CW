#!/bin/bash

# Django Server-Run Script

# Function to print bold text
print_bold() {
    echo -e "\033[1m$1\033[0m"
}

create_venv() { # ! -d checks if given directory does NOT exist
    cd casino_simulator_api
    if [ ! -d "venv" ]; then
        print_bold "\nCreating virtual environment...\n"
        python3 -m venv venv
        source venv/bin/activate
        pip install --upgrade pip
        pip install -r requirements.txt
    else
        print_bold "Virtual environment already exists. Activating..."
        source venv/bin/activate
    fi
    cd ..
}

# Function to run the server
run_server() {
    cd casino_simulator_api
    print_bold "Running the server...\n"
    python3 manage.py runserver || print_bold "Failed to run the server" # if command fails print message
    #python manage.py runserver 0.0.0.0:8000
}

# Function to migrate before running the server
migrate() {
    bash scripts/migrate.sh
}

# Main function 
main() {
    create_venv
    if migrate; then
        print_bold "\nMigrations successful. Running the server...\n"
        run_server
    else
        print_bold "\nMigrations failed\n"
        exit 1
    fi
}

main

# n2o bash and zsh completion
complete -o default -W "$(n2o help 2>&1 | tail -n +2 | grep -v ^=== | cut -f1 -d' ')" n2o

// The example program from the lab slide (fork + exec + wait), with the
// missing #include lines filled in.
// compile: gcc fork_exec.c -o fork_exec
// run:     ./fork_exec

#include <stdio.h>      // printf, perror
#include <unistd.h>     // fork, execlp
#include <sys/types.h>  // pid_t
#include <sys/wait.h>   // wait

int main() {
    pid_t pid = fork();

    if (pid < 0) {
        // fork() returned -1: no child was created
        perror("Fork failed");
        return 1;
    } else if (pid == 0) {
        // Child (pid == 0): replace this program with /bin/ls.
        // If exec works, the next line is never reached.
        execlp("/bin/ls", "ls", NULL);
        perror("exec failed");
        return 1;
    } else {
        // Parent (pid == the child's pid): wait until the child finishes
        wait(NULL);
        printf("Child Complete\n");
    }
    return 0;
}

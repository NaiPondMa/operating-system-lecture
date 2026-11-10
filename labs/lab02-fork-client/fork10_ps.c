// Same idea as fork10.c, but slower, so you have time to look at the
// processes with `ps` and stop them with `kill`.
// Every process also prints its parent's pid (getppid).
// compile: gcc fork10_ps.c -o fork10_ps
// run:     ./fork10_ps
// then, in a SECOND terminal:  ps -f   or   ps --ppid <parent pid>   or   pstree -p <parent pid>

#include <stdio.h>      // printf, perror, fflush
#include <stdlib.h>     // exit
#include <unistd.h>     // fork, getpid, getppid, sleep
#include <sys/types.h>  // pid_t
#include <sys/wait.h>   // wait

int main() {
    int n = 10;

    // The parent's parent is the shell (bash). Compare with `ps` / `echo $$`.
    printf("Parent started (pid %d), its parent is bash (pid %d)\n",
           getpid(), getppid());
    fflush(stdout);  // print now, so the line is not copied into the children

    for (int i = 0; i < n; i++) {
        pid_t pid = fork();

        if (pid < 0) {
            perror("Fork failed");
            return 1;
        } else if (pid == 0) {
            printf("I'm the child number %d (pid %d, parent pid %d)\n",
                   i, getpid(), getppid());
            fflush(stdout);
            sleep(30);   // stay alive for 30 seconds: go and look at it with ps
            exit(0);
        }
    }

    for (int i = 0; i < n; i++) {
        wait(NULL);
    }

    printf("Parent terminates (pid %d)\n", getpid());
    return 0;
}

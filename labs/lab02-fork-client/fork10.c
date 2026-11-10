// Lab 2, activity #2 (Linux only): create 10 children.
// compile: gcc fork10.c -o fork10
// run:     ./fork10

#include <stdio.h>      // printf, perror
#include <stdlib.h>     // exit
#include <unistd.h>     // fork, getpid  (also getppid, sleep, execlp)
#include <sys/types.h>  // pid_t
#include <sys/wait.h>   // wait

int main() {
    int n = 10;

    for (int i = 0; i < n; i++) {
        pid_t pid = fork();

        if (pid < 0) {
            perror("Fork failed");
            return 1;
        } else if (pid == 0) {
            // โค้ดส่วนของ Child process
            printf("I'm the child number %d (pid %d)\n", i, getpid());
            
            // สำคัญมาก: ต้อง exit เพื่อไม่ให้ child วนลูปไป fork หลานต่อ
            exit(0); 
        }
        // Parent จะวนลูปสร้าง child ตัวถัดไปจนครบ 10 ตัว
    }

    // Parent รอให้ Child ทั้ง 10 ตัวทำงานเสร็จทั้งหมด
    for (int i = 0; i < n; i++) {
        wait(NULL);
    }

    // Parent พิมพ์ข้อความปิดท้ายหลังลูกทั้งหมดจบการทำงาน
    printf("Parent terminates (pid %d)\n", getpid());

    return 0;
}
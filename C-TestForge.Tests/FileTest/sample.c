/**
 * sample.c - Sample C file for testing C-TestForge parser
 *
 * This file contains various C constructs to test the parser's capabilities
 */

#include <stdio.h>
#include <stdlib.h>
#include <string.h>

/* Define some constants */
#define MAX_SIZE 100
#define MIN_SIZE 10
#define PI 3.14159
#define VERSION "1.0.0"

/* Macro with arguments */
#define MAX(a, b) ((a) > (b) ? (a) : (b))
#define MIN(a, b) ((a) < (b) ? (a) : (b))
#define SQR(x) ((x) * (x))
#define PRINT_DEBUG(msg) printf("[DEBUG] %s\n", msg)

/* Conditional compilation */
#define DEBUG 1

#ifdef DEBUG
#define LOG(msg) printf("[LOG] %s\n", msg)
#else
#define LOG(msg)
#endif

/* Typedef and enum */
typedef enum {
    SUNDAY = 0,
    MONDAY,
    TUESDAY,
    WEDNESDAY,
    THURSDAY,
    FRIDAY,
    SATURDAY
} DayOfWeek;

typedef struct {
    int id;
    char name[50];
    float score;
} Student;

/* Global variables */
int g_counter = 0;
const float g_pi = 3.14159;
static char g_buffer[MAX_SIZE];
static int g_initialized = 0;

#if defined(DEBUG) && (DEBUG > 0)
int g_debugMode = 1;
#else
int g_debugMode = 0;
#endif

/* Function prototypes */
void initialize(void);
int add(int a, int b);
float multiply(float a, float b);
char* copyString(const char* source);
void printStudent(const Student* student);
DayOfWeek getNextDay(DayOfWeek day);

/**
 * Initialize the application
 */
void initialize(void) {
    if (g_initialized) {
        return;
    }
    
    LOG("Initializing application");
    
    memset(g_buffer, 0, MAX_SIZE);
    g_counter = 0;
    g_initialized = 1;
    
    PRINT_DEBUG("Initialization complete");
}

/**
 * Add two integers
 * 
 * @param a First integer
 * @param b Second integer
 * @return Sum of a and b
 */
int add(int a, int b) {
    LOG("Adding two numbers");
    return a + b;
}

/**
 * Multiply two floating point numbers
 * 
 * @param a First number
 * @param b Second number
 * @return Product of a and b
 */
float multiply(float a, float b) {
    LOG("Multiplying two numbers");
    return a * b;
}

/**
 * Copy a string to a new dynamically allocated string
 * 
 * @param source Source string
 * @return Newly allocated string containing a copy of source
 */
char* copyString(const char* source) {
    if (source == NULL) {
        return NULL;
    }
    
    size_t length = strlen(source);
    char* destination = (char*)malloc(length + 1);
    
    if (destination != NULL) {
        strcpy(destination, source);
    }
    
    return destination;
}

/**
 * Print student information
 * 
 * @param student Pointer to Student structure
 */
void printStudent(const Student* student) {
    if (student == NULL) {
        printf("Invalid student\n");
        return;
    }
    
    printf("Student ID: %d\n", student->id);
    printf("Name: %s\n", student->name);
    printf("Score: %.2f\n", student->score);
}

/**
 * Get the next day of the week
 * 
 * @param day Current day
 * @return Next day of the week
 */
DayOfWeek getNextDay(DayOfWeek day) {
    return (day + 1) % 7;
}

/**
 * Main function
 */
int main(int argc, char** argv) {
    initialize();
    
    printf("Sample C application v%s\n", VERSION);
    
    int sum = add(5, 3);
    printf("5 + 3 = %d\n", sum);
    
    float product = multiply(2.5f, 4.0f);
    printf("2.5 * 4.0 = %.2f\n", product);
    
    int max = MAX(10, 20);
    printf("MAX(10, 20) = %d\n", max);
    
    char* text = copyString("Hello, World!");
    printf("Copied string: %s\n", text);
    free(text);
    
    Student student;
    student.id = 12345;
    strcpy(student.name, "John Doe");
    student.score = 92.5f;
    
    printStudent(&student);
    
    DayOfWeek today = MONDAY;
    DayOfWeek tomorrow = getNextDay(today);
    printf("Today is day %d, tomorrow is day %d\n", today, tomorrow);
    
    /* Conditional code */
#ifdef DEBUG
    printf("Debug mode is enabled\n");
#else
    printf("Debug mode is disabled\n");
#endif
    
    /* Loop examples */
    for (int i = 0; i < 5; i++) {
        printf("Loop iteration %d\n", i);
    }
    
    int counter = 0;
    while (counter < 3) {
        printf("While loop iteration %d\n", counter);
        counter++;
    }
    
    printf("Application completed successfully\n");
    return 0;
}
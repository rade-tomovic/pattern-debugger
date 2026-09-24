#ifndef NAME
#define NAME sum_to
#endif
int NAME(int n)
{
    int total = 0;
    for (int i = 1; i <= n; i++)
        total += i;
    return total;
}

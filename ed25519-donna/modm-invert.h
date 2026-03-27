/*
	derived from libsodium, ISC license
*/

STATIC inline void
sq256_modm(bignum256modm s, const bignum256modm a)
{
    mul256_modm(s, a, a);
}

STATIC inline void
sqmul256_modm(bignum256modm s, const int n, const bignum256modm a)
{
    int i;

    for (i = 0; i < n; i++) {
        sq256_modm(s, s);
    }
    mul256_modm(s, s, a);
}

STATIC void
invert256_modm(bignum256modm recip, const bignum256modm s)
{
    bignum256modm _10, _100, _11, _101, _111, _1001, _1011, _1111;

    sq256_modm(_10, s);
    sq256_modm(_100, _10);
    mul256_modm(_11, _10, s);
    mul256_modm(_101, _10, _11);
    mul256_modm(_111, _10, _101);
    mul256_modm(_1001, _10, _111);
    mul256_modm(_1011, _10, _1001);
    mul256_modm(_1111, _100, _1011);
    mul256_modm(recip, _1111, s);

    sqmul256_modm(recip, 123 + 3, _101);
    sqmul256_modm(recip, 2 + 2, _11);
    sqmul256_modm(recip, 1 + 4, _1111);
    sqmul256_modm(recip, 1 + 4, _1111);
    sqmul256_modm(recip, 4, _1001);
    sqmul256_modm(recip, 2, _11);
    sqmul256_modm(recip, 1 + 4, _1111);
    sqmul256_modm(recip, 1 + 3, _101);
    sqmul256_modm(recip, 3 + 3, _101);
    sqmul256_modm(recip, 3, _111);
    sqmul256_modm(recip, 1 + 4, _1111);
    sqmul256_modm(recip, 2 + 3, _111);
    sqmul256_modm(recip, 2 + 2, _11);
    sqmul256_modm(recip, 1 + 4, _1011);
    sqmul256_modm(recip, 2 + 4, _1011);
    sqmul256_modm(recip, 6 + 4, _1001);
    sqmul256_modm(recip, 2 + 2, _11);
    sqmul256_modm(recip, 3 + 2, _11);
    sqmul256_modm(recip, 3 + 2, _11);
    sqmul256_modm(recip, 1 + 4, _1001);
    sqmul256_modm(recip, 1 + 3, _111);
    sqmul256_modm(recip, 2 + 4, _1111);
    sqmul256_modm(recip, 1 + 4, _1011);
    sqmul256_modm(recip, 3, _101);
    sqmul256_modm(recip, 2 + 4, _1111);
    sqmul256_modm(recip, 3, _101);
    sqmul256_modm(recip, 1 + 2, _11);
}

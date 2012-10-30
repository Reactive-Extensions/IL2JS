function Log(msg) {
    document.write("<tt>" + msg.toString().replace(/</g, "&lt;") + "</tt><br/>");
}

function Primes(n) {
    this.primes = new Array(n);

    this.enumPrimes = function() {
        this.found(0, 1);
        this.found(1, 2);
        var candidate = 1;
        for (var i = 2; i < this.primes.length; i++) {
            do
                candidate += 2;
            while (!this.isPrime(candidate));
            this.found(i, candidate);
        }
    };

    this.found = function(i, prime) {
        this.primes[i] = prime;
        if (i > 0 && i % 1000 == 0)
            Log(prime + " is prime...");
    };

    this.isPrime = function(candidate) {
        var i = 1;
        while (true) {
            var p = this.primes[i];
            if (candidate < p * p)
                return true;
            if (candidate % p == 0)
                return false;
            i++;
        }
    };

    this.Count = function() { return this.primes.length; };
    this.Index = function(i) { return this.primes[i]; };

    this.enumPrimes();
}

// Following based on:
//   Simon Plouffe, "On the Computation of the n'th decimal digit of various transcendental numbers" (Nov 1996).
// Written in C by by Fabrice Bellard (Jan 1997).

var knownDigits = "3.141592653589793238462643383279502884197169399375105820974944592307816406286208998628034825342117067982148086513282306647093844609550582231725359408128481117450284102701938521105559644622948954930381964428810975665933446128475648233786783165271201909145648566923460348610454326648213393607260249141273724587006606315588174881520920962829254091715364367892590360011330530548820466521384146951941511609433057270365759591953092186117381932611793105118548074462379962749567351885752724891227938183011949129833673362440656643086021394946395224737190702179860943702770539217176293176752384674818467669405132000568127145263560827785771342757789609173637178721468440901224953430146549585371050792279689258923542019956112129021960864034418159813629774771309960518707211349999998372978";

function fmod(a, m) {
    return a % m;
}

function inv_mod(x, y) {
    var u = x;
    var v = y;
    var c = 1;
    var a = 0;
    do {
        var q = (v / u) << 0;
        var t = c;
        c = a - (q * c);
        a = t;
        t = u;
        u = v - (q * u);
        v = t;
    }
    while (u != 0);
    a = a % y;
    if (a < 0)
        a = y + a;
    return a;
}

function mul_mod(a, b, m) {
    return (a * b) % m;
}

function pow_mod(a, b, m) {
    var r = 1;
    var aa = a;
    while (true) {
        if ((b & 1) != 0)
            r = mul_mod(r, aa, m);
        b = b >> 1;
        if (b == 0)
            return r;
        aa = mul_mod(aa, aa, m);
    }
}

function nineDigitsAt(primes, n) {
    var N = ((((n + 20) * Math.log(10.0)) / Math.log(2.0))) << 0;
    var sum = 0.0;
    var primeIndex = 2;
    while (true) {
        if (primeIndex >= primes.Count())
            return null;
        var a = primes.Index(primeIndex++);
        if (a > (2 * N)) {
            var digits = (sum * 1e9) << 0;
            var str = "000000000" + digits;
            return str.substr(str.length - 9);
        }
        var vmax = ((Math.log((2 * N)) / Math.log(a))) << 0;
        var av = 1;
        var i = 0;
        while (i < vmax) {
            av *= a;
            i++;
        }
        var s = 0;
        var num = 1;
        var den = 1;
        var v = 0;
        var kq = 1;
        var kq2 = 1;
        for (var k = 1; k <= N; k++) {
            var t = k;
            if (kq >= a) {
                do {
                    t = (t / a) << 0;
                    v--;
                }
                while ((t % a) == 0);
                kq = 0;
            }
            kq++;
            num = mul_mod(num, t, av);
            t = (2 * k) - 1;
            if (kq2 >= a) {
                if (kq2 == a) {
                    do {
                        t = (t / a) << 0;
                        v++;
                    }
                    while ((t % a) == 0);
                }
                kq2 -= a;
            }
            den = mul_mod(den, t, av);
            kq2 += 2;
            if (v > 0) {
                t = mul_mod(mul_mod(inv_mod(den, av), num, av), k, av);
                for (i = v; i < vmax; i++)
                    t = mul_mod(t, a, av);
                s += t;
                if (s >= av)
                    s -= av;
            }
        }
        s = mul_mod(s, pow_mod(10, n - 1, av), av);
        sum = fmod(sum + (s / av), 1.0);
    }
}

function Emit(primes) {
    Log("3.");
    var i = 2;
    while (true) {
        if (i + 9 > knownDigits.length) {
            Log("Ran out of known digits to check against");
            return;
        }
        var digits = nineDigitsAt(primes, i - 1);
        if (digits == null) {
            Log("Ran out of primes");
            return;
        }
        if (digits != knownDigits.substr(i, 9))
            throw new Error("calculated digits do not agree with actual digits");
        i += 9;
        Log(digits);
    }
}

function ReadInt(number) {
    var val = parseInt(number);
    Log(number + " => " + val);
    return val;
}

function ReadDouble(number) {
    var val = parseFloat(number);
    Log(number + " => " + val);
    return val;
}

function Unary(a, b) {
    Log(a + ", " + b);
}

function Main() {
    var then = new Date();

    Log("Calculating primes...");
    var primes = new Primes(10001);

    Log("Calculating pi...");
    Emit(primes);

    var now = new Date();
    Log("Duration: " + (now - then) + "ms");
}

Main();


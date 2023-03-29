# dFunc (Disfunctional Functional Programming Language)


## What's new?

* The list type has been added
* ...


## Example
An example program can be found in `Examples/example1.df`. It contains the following program:

```hs
len := (xs: [real]) -> real
	=> _len(xs, 0)
	;

_len := (xs: [real], res: real) -> real
	| xs = [] => res
	| else => _len(tail(xs), res + 1)
	;


main := () -> real
	=> len([1, 2, 3, 4]);
	;
```

When executed, it output `4`.


## Todo:

* Add function types and function argument passing
* Add integer data type
* Add TCO
* expand stdlib
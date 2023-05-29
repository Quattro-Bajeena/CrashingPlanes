# Badanie efektywności algorytmu unikania kolizji w przestrzeni powietrznej
Mateusz Oleszek, nr. 144608

## Parametry
Zmienna niezależna:
Ilość samolotów, zakres wszystkich liczb od 10 do 45.

Zmienna zależna:
Czas, Czy zostało znalezione rozwiązanie optymalne.


Ilość powtórzeń eksperymentu dla tych samych parametrów: 5

## Instancje testowe
Wygenerowałem instancje testowe o podobnej gęstości jak te dostarczone z laboratorium, czyli o gęstości jedynek około 6.5%. Zostały zachowane zasady wynikające z przedstawiania macierzy konfliktów samolotów, tzn. liczby na przekątnej to 1, a w intersekcjach przecięć dla tego samego samolotu 0.

## Wykresy

![](F:\Programowanie\Studia\Geografia\CrashingPlanes\Images\Screenshot_6190_EXCEL-2023_05_30-01_26.png)

## Wnioski

Program zdołał znaleźć rozwiązanie optymalne dla każdej instancji testowej. Czas jaki zajmuje algorytmowi rozwiązanie zadania ma charakterystykę funkcji wykładniczej lub nawet silniowej. Z rządami wielkości czasu skaczącymi z poziomu sekundy, do kilkunastu, do kilkuset przez zmianę ilości samolotów o kilka. Widać też jednak sporą wariancję w czasach dla wyższej wielkości macierzy, jako że trudność danego problemu optymalizacyjnego zależy mocno od samej konstrukcji równań, które tutaj były losowo generowane.

Czasy dla ilości samolotów powyżej 50 były zbyt wysokie żeby kilka razy je mierzyć, ale za każdym razem nawet po kilku minutach obliczeń udawało się znaleźć optymalne rozwiązanie.

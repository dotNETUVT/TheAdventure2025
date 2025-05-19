
---

# TheAdventure – Rezumat Pull Request 1 (actual)

## Ce s-a rezolvat si adaugat pana acum

In aceasta etapa a dezvoltarii, s-au facut mai multe imbunatatiri pentru gameplay:

### Corecturi importante

* Playerul nu mai poate iesi in afara hartii. Miscarea este acum limitata la dimensiunile terenului.
* Harta a fost extinsa pentru a permite explorarea unui spatiu mai mare.
* Jocul ruleaza acum in modul fullscreen.
* Apasarea tastei ESC inchide jocul.

### Mecanism de dificultate dinamica

Dificultatea jocului creste treptat, o data la 30 de secunde. In functie de nivelul atins:

* Se genereaza mai multe bombe (maximum 10 in acelasi timp).
* Bombele apar mai aproape de jucator, dar random.
* Intervalul de timp dintre aparitii scade pe masura ce jocul avanseaza, dar este limitat pentru a preveni comportamente nedorite.

### Efecte vizuale si audio

* Atunci cand o bomba explodeaza (si este eliminata din scena), camera tremura.
* In acel moment se reda si un sunet de explozie.
* Este important de mentionat ca aceste efecte (shake si sunet) se aplica o singura data, doar pentru bomba respectiva. Nu sunt aplicate pentru toate bombele, pentru a mentine eficienta jocului.

### Limitarea actiunilor jucatorului

* Jucatorul are un numar limitat de 10 bombe pe care le poate plasa (prin tasta b sau click).
* Aceste bombe vor fi utile mai tarziu pentru a ajuta la colectarea bonusurilor din joc (cand playerul nu mai poate ataca cu abia).

### Alte ajustari

* Raza de efect a bombelor a fost marita pentru a sustine un gameplay mai palpitant, in special cand jucatorul urmareste sa colecteze obiecte.

---

## Ce urmeaza (pregatire pentru Pull Request 2)

* Introducerea bonusurilor care apar random in jurul bombelor generate automat, pentru a atrage jucatorul in zone riscante.
* Adaugarea de noi pericole care apar odata cu cresterea dificultatii (optional).
* Afisarea timpului supravietuit si a scorului (bazat pe bonusuri colectate) (optional, nu mi-a iesit la acest PR).
* Inchiderea automata a jocului cand jucatorul pierde, oprind totodata si generarea de bombe.
* Adaugarea unui buton de restart (replay).
* Implementarea unei bombe speciale, de sacrificiu, pe care jucatorul o poate plasa pentru a accesa un bonus periculos. Aceasta bomba va avea o explozie mai rapida si va putea afecta si jucatorul.

---
/******************************************************************************/
/****  Converted from Firebird metadata script (Bookstore2) for SQLite     ****/
/****  Source: Bookstore2_2026-05-27_Firebird_entire_db_metadata_script.sql ****/
/****  Generated: 2026-05-27                                               ****/
/******************************************************************************/

PRAGMA foreign_keys = ON;


/******************************************************************************/
/****                                Tables                                ****/
/******************************************************************************/


CREATE TABLE cities (
    id    INTEGER NOT NULL PRIMARY KEY AUTOINCREMENT,
    name  TEXT
);

CREATE TABLE groups (
    id    INTEGER NOT NULL PRIMARY KEY AUTOINCREMENT,
    name  TEXT
);

CREATE TABLE languages (
    id    INTEGER NOT NULL PRIMARY KEY AUTOINCREMENT,
    name  TEXT
);

CREATE TABLE publishers (
    id    INTEGER NOT NULL PRIMARY KEY AUTOINCREMENT,
    name  TEXT
);

CREATE TABLE shops (
    id    INTEGER NOT NULL PRIMARY KEY AUTOINCREMENT,
    name  TEXT
);

CREATE TABLE books (
    id              INTEGER NOT NULL PRIMARY KEY AUTOINCREMENT,
    crt_date_time   TEXT NOT NULL DEFAULT (datetime('now', 'localtime')),
    author          TEXT,
    title           TEXT NOT NULL,
    publisher_id    INTEGER NOT NULL DEFAULT 0,
    page_count      INTEGER,
    publish_year    INTEGER,
    edition         INTEGER NOT NULL DEFAULT 1,
    format          TEXT,
    isbn            TEXT,
    price           REAL,
    date_when_get   TEXT,
    wrapper         INTEGER NOT NULL DEFAULT 0,
    language_id     INTEGER NOT NULL DEFAULT 0,
    group_id        INTEGER,
    shop_id         INTEGER NOT NULL DEFAULT 0,
    city_id         INTEGER NOT NULL DEFAULT 0,
    has_digit_copy  INTEGER NOT NULL DEFAULT 0,
    annotation      TEXT,
    details         TEXT,
    cover_image     BLOB,
    book_file       TEXT,
    CONSTRAINT fk_books_to_cities FOREIGN KEY (city_id) REFERENCES cities (id),
    CONSTRAINT fk_books_to_groups FOREIGN KEY (group_id) REFERENCES groups (id),
    CONSTRAINT fk_books_to_languages FOREIGN KEY (language_id) REFERENCES languages (id),
    CONSTRAINT fk_books_to_publishers FOREIGN KEY (publisher_id) REFERENCES publishers (id),
    CONSTRAINT fk_books_to_shops FOREIGN KEY (shop_id) REFERENCES shops (id)
);


/******************************************************************************/
/****                                 Indexes                              ****/
/******************************************************************************/


CREATE INDEX idx_books_city_id ON books (city_id);
CREATE INDEX idx_books_group_id ON books (group_id);
CREATE INDEX idx_books_language_id ON books (language_id);
CREATE INDEX idx_books_publisher_id ON books (publisher_id);
CREATE INDEX idx_books_shop_id ON books (shop_id);


/******************************************************************************/
/****                             Descriptions                             ****/
/****  (converted from Firebird DESCRIBE statements)                       ****/
/******************************************************************************/

-- Table books: Таблиця книг.
-- Table cities: Міста, в яких видано книгу.
-- Table groups: Групи книг (наприклад: Програмування, Софт, Залізо і т.д.).
-- Table languages: Мови, на яких видані книги.
-- Table publishers: Видавці (видавництва).
-- Table shops: Магазини, в якмх придбано книги.

-- Field books.id: Унікальний ідентифікатор (первинний ключ).
-- Field books.crt_date_time: Дата і час створення запису.
-- Field books.author: Автор книги.
-- Field books.title: Назва книги.
-- Field books.publisher_id: Іденитифікатор видавця (посилання на таблицю publishers).
-- Field books.page_count: Кількість сторінок.
-- Field books.publish_year: Рік видання книги.
-- Field books.edition: Номер видання.
-- Field books.format: Формат книги. Наприклад: 70x100/16. Або висота і ширина в см.
-- Field books.isbn: ISBN.
-- Field books.price: Ціна книги на момент придбання (в гривнях).
-- Field books.date_when_get: Дата отримання книги.
-- Field books.wrapper: Обкладинка: 0 - м'яка, 1 - тверда.
-- Field books.language_id: Мова книги (посилання на таблицю languages).
-- Field books.group_id: Група до якої віднесено книгу (посилання на таблицю groups).
-- Field books.shop_id: Магазин, в якому придбана книга (посилання на таблицю shops).
-- Field books.city_id: Місто, в якому видано книгу (посилання на таблицю cities).
-- Field books.has_digit_copy: Чи є цифрова (електронна) версія книги: 0 - немає, 1 - є.
-- Field books.annotation: Анотація.
-- Field books.details: Примітки.
-- Field books.cover_image: Зображення титульної сторінки.
-- Field books.book_file: Файл книги (повний шлях).

-- Field cities.id: Унікальний ідентифікатор (первинний ключ).
-- Field cities.name: Назва.

-- Field groups.id: Унікальний ідентифікатор групи (первинний ключ).
-- Field groups.name: Назва групи.

-- Field languages.id: Унікальний ідентифікатор (первинний ключ).
-- Field languages.name: Назва мови.

-- Field publishers.id: Унікальний ідентифікатор (первинний ключ).
-- Field publishers.name: Назва видавця (видавництва).

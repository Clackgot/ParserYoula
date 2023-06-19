# ParserYoula
Консольное приложение для выгрузки объявлений с [Юлы](https://youla.ru).

**Возможности:**
- Многопоточность
- Запись объявлений в базу данных для исключения объявлений при повторных поисках
- Создание файла с найденными объявлениями в формате Excel с сортировкой по дате
- Возможность прервать выполнение программы с сохранением результатов работы
- Поиск по всем городам
- Настройка фильтра поиска в формате JSON
```JSON
{
    "MinRatingCount": 0,
    "MaxRatingCount": 2,
    "BlackwordsTitle": [
        "Слово_в_названии",
        "Фраза в названии"
    ],
    "BlackwordsDescription": [
        "Слово_в_описании",
        "Фраза в описании"
    ],
    "withShops": false
}
```

**Скриншоты:**
![image](https://github.com/Clackgot/ParserYoula/assets/30325670/34734070-01fa-43a0-8d1f-f9e637778a1e)
![image](https://github.com/Clackgot/ParserYoula/assets/30325670/070c3124-f6e0-4a00-9a70-33d6c5202b38)
![image](https://github.com/Clackgot/ParserYoula/assets/30325670/c5267ac8-2e27-4542-b7ec-055026c29ce3)

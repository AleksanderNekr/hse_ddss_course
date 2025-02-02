import requests
from bs4 import BeautifulSoup
from selenium import webdriver
from selenium.webdriver.common.by import By
from selenium.webdriver.support.ui import WebDriverWait
from selenium.webdriver.support import expected_conditions as EC

def format_date(day, month, year):
    return f"{day}.{month}.{year}"

def parse_calendar_ru(year):

    months = {
        "Январь": 1,
        "Февраль": 2,
        "Март": 3,
        "Апрель": 4,
        "Май": 5,
        "Июнь": 6,
        "Июль": 7,
        "Август": 8 ,
        "Сентябрь": 9,
        "Октябрь": 10,
        "Ноябрь": 11,
        "Декабрь": 12,
    }

    # URL страницы с календарем
    url = f"https://www.consultant.ru/law/ref/calendar/proizvodstvennye/{year}/"

    # Получаем HTML-контент страницы
    response = requests.get(url)
    soup = BeautifulSoup(response.content, "html.parser")

    # Инициализируем словарь для хранения данных по месяцам
    calendar = {"holidays": [], "pre_holidays": []}

    # Находим все таблицы с классом "cal"
    tables = soup.find_all("table", class_="cal")

    # Обрабатываем каждую таблицу
    for table in tables:
        # Извлекаем название месяца
        month = months.get(table.find("th", class_="month").text.strip())

        # Находим все ячейки таблицы (<td>)
        days = table.find_all("td")

        for day in days:
            day_text = day.text.strip()
            if not day_text or day_text == "&nbsp;":  # Пропускаем пустые ячейки
                continue

            # Определяем тип дня по классам
            if "holiday" in day.get("class", []) or "weekend" in day.get("class", []):
                calendar["holidays"].append(format_date(day_text, month, year))
            elif "preholiday" in day.get("class", []):
                calendar["pre_holidays"].append(format_date(day_text.removesuffix('*'), month, year))

    return calendar


def parse_calendar_me(year):
    months_dict = {
        "Januar": 1,
        "Februar": 2,
        "Mart": 3,
        "April": 4,
        "Maj": 5,
        "Juni": 6,
        "Juli": 7,
        "Avgust": 8 ,
        "Septembar": 9,
        "Oktobar": 10,
        "Novembar": 11,
        "Decembar": 12,
    }

    # Настройки браузера для работы в Colab
    options = webdriver.ChromeOptions()
    options.add_argument("--headless")  # Запускаем браузер в фоновом режиме
    options.add_argument("--no-sandbox")
    options.add_argument("--disable-dev-shm-usage")

    # Запускаем браузер
    driver = webdriver.Chrome(options=options)

    # URL страницы
    url = f"https://neradni-dani.com/kalendar-{year}-cg.php"

    # Загружаем страницу
    driver.get(url)

    # Ждем, пока страница полностью загрузится
    wait = WebDriverWait(driver, 10)  # Ждем максимум 10 секунд
    wait.until(EC.presence_of_element_located((By.ID, "calendar")))

    # Извлекаем отрендеренный HTML-код страницы
    page_source = driver.page_source

    # Закрываем браузер
    driver.quit()

    # Разбираем HTML с помощью BeautifulSoup
    soup = BeautifulSoup(page_source, "html.parser")

    # Список для сбора нерабочих дней
    non_working_days = []

    # Признаки стилей нерабочих дней
    target_styles = [
        "box-shadow: rgb(255, 74, 50)",  # Красная тень
        "color: rgb(6, 70, 133)",       # Синий текст
        "color: rgb(240, 0, 0)"         # Красный текст
    ]

    # Поиск всех месяцев
    months = soup.find_all("table", class_="month")

    for month in months:
        # Получаем заголовок месяца
        month_name = month.find("th", class_="month-title").text.split()[0].strip()

        # Перебираем все дни в месяце
        for day_cell in month.find_all("td", class_="day"):
            style = day_cell.get("style", "")
            day_content = day_cell.find("div", class_="day-content")

            if day_content:
                day_number = day_content.text.strip()

                # Проверяем стиль или цвет на признаки нерабочих дней
                if any(target in style for target in target_styles) or "color:" in day_content.get("style", ""):
                    non_working_days.append(format_date(day_number, months_dict.get(month_name), year))

    return {"holidays": non_working_days, "pre_holidays": []}

if __name__ == "__main__":
    print(parse_calendar_ru(2025))
    print('\n', parse_calendar_me(2025))
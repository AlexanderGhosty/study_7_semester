import pytest
from pages.contact_page import ContactPage


def test_negative_empty_required_email(driver):
    """Отправка формы с пустым обязательным полем email."""
    page = ContactPage(driver)
    page.open_page()
    page.fill_form("Alex", "Bykov", "", "Male", "1234567890")

    page.submit_form()

    email_field = page.find(page.EMAIL)
    validation_message = email_field.get_attribute("validationMessage")

    assert validation_message
    assert not driver.find_elements(*page.SUCCESS_MODAL)

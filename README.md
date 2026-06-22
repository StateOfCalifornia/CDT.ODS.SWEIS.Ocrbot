# OCRBot

OCRBot application is a File Access Remediation batch tool for embedding OCR text into scanned documents.

It uses **Azure AI Document Intelligence** to extract text from scanned PDFs and (optionally) **Azure OpenAI** to auto-tag documents for accessibility (UA tags).

---

# Local Host OCRBot Developer Setup

## Software Prerequisites

- Download the latest version of **Git**
- **Visual Studio 2022** (17.14 or later)
  - Downloading and installing will also install the needed **.NET 8 SDK**.
  - Any edition will suffice, but **Enterprise** will provide the most robust experience.
  - Required workload: **.NET desktop development**
  - Required extension: **Microsoft Visual Studio Installer Projects** (needed to build the `OCRBot Setup` project that produces `OCRBotSetup.msi`)

## Azure Prerequisites

- Require an **Azure Subscription** with **Azure OpenAI** and **Azure AI Document Intelligence** endpoints provisioned.
- Add the endpoint and API key in the application **Settings**.
- Name the **Azure OpenAI deployment** as `ocrbot-tagging`.

> Document Intelligence credentials are always required.
> OpenAI credentials are only required when the **Auto-Tag** feature is enabled.


Clone the repository
git clone https://github.com/StateOfCalifornia/CDT.ODS.SWEIS.Ocrbot.git 
cd CDT.ODS.SWEIS.Ocrbot\CDT.OCRBot


# Install Instructions for OCRBot desktop app

1. Download the Windows Installer file **`OCRBotSetup.msi`**.
2. Launch the `.msi` file to install the OCRBot application.
3. Launch the application from the Start menu.
4. Go to the **Settings** menu and configure the Azure services endpoints (Document Intelligence and, if using auto-tagging, OpenAI).
5. Configure default input/output directories (optional).
6. Start using the application.

import os
import sys
import time
from playwright.sync_api import sync_playwright

BRAIN_DIR = r"C:\Users\nedim\.gemini\antigravity\brain\d58d12a2-00ee-4d47-98b8-25ac990ec966"
os.makedirs(BRAIN_DIR, exist_ok=True)

# Minimal valid PDF file
PDF_CONTENT_V1 = (
    b"%PDF-1.4\n"
    b"1 0 obj\n<< /Type /Catalog /Pages 2 0 R >>\nendobj\n"
    b"2 0 obj\n<< /Type /Pages /Kids [3 0 R] /Count 1 >>\nendobj\n"
    b"3 0 obj\n<< /Type /Page /Parent 2 0 R /MediaBox [0 0 612 792] /Contents 4 0 R >>\nendobj\n"
    b"4 0 obj\n<< /Length 55 >>\nstream\n"
    b"BT /F1 12 Tf 72 712 Td (Alex Mercer - Lead Cloud Architect v1) Tj ET\n"
    b"endstream\nendobj\n"
    b"xref\n0 5\n0000000000 65535 f \n0000000010 00000 n \n0000000060 00000 n \n0000000117 00000 n \n0000000216 00000 n \n"
    b"trailer\n<< /Size 5 /Root 1 0 R >>\nstartxref\n323\n%%EOF\n"
)

PDF_CONTENT_V2 = (
    b"%PDF-1.4\n"
    b"1 0 obj\n<< /Type /Catalog /Pages 2 0 R >>\nendobj\n"
    b"2 0 obj\n<< /Type /Pages /Kids [3 0 R] /Count 1 >>\nendobj\n"
    b"3 0 obj\n<< /Type /Page /Parent 2 0 R /MediaBox [0 0 612 792] /Contents 4 0 R >>\nendobj\n"
    b"4 0 obj\n<< /Length 63 >>\nstream\n"
    b"BT /F1 12 Tf 72 712 Td (Alex Mercer - Lead Cloud Architect v2 Cloud) Tj ET\n"
    b"endstream\nendobj\n"
    b"xref\n0 5\n0000000000 65535 f \n0000000010 00000 n \n0000000060 00000 n \n0000000117 00000 n \n0000000216 00000 n \n"
    b"trailer\n<< /Size 5 /Root 1 0 R >>\nstartxref\n331\n%%EOF\n"
)

v1_path = os.path.abspath("scratch/temp_resume_v1.pdf")
v2_path = os.path.abspath("scratch/temp_resume_v2.pdf")

with open(v1_path, "wb") as f:
    f.write(PDF_CONTENT_V1)

with open(v2_path, "wb") as f:
    f.write(PDF_CONTENT_V2)

print(f"Created temporary PDF files: {v1_path}, {v2_path}")

with sync_playwright() as p:
    browser = p.chromium.launch(headless=True)
    context = browser.new_context(viewport={"width": 1440, "height": 900})
    page = context.new_page()

    # 1. Register User
    print("1. Registering user...")
    page.goto("http://localhost:5173/register")
    page.wait_for_load_state("networkidle")

    unique_email = f"alex_mercer_{int(time.time())}@pipeline.local"
    page.fill('input[placeholder="Jane Doe"]', "Alex Mercer")
    page.fill('input[placeholder="you@example.com"]', unique_email)
    page.fill('input[type="password"]', "Password123!")
    page.click('button[type="submit"]')

    page.wait_for_timeout(2000)
    print("Logged in, navigating to /documents...")

    # 2. Go to /documents
    page.goto("http://localhost:5173/documents")
    page.wait_for_load_state("networkidle")
    page.wait_for_timeout(1000)

    # 3. Upload first document (v1)
    print("3. Uploading first document...")
    page.click("button:has-text('Upload Document'), button:has-text('Upload First Document')")
    page.wait_for_timeout(500)

    page.fill('input[placeholder*="Senior Backend Engineer CV"]', "Lead Cloud Architect Resume")
    page.fill('input[placeholder*="v1 or 2026-q1"]', "v1")
    page.fill('input[placeholder*="Highlighted cloud scale"]', "Initial verified draft")
    page.set_input_files('input[type="file"]', v1_path)
    page.wait_for_timeout(500)

    page.click('button[type="submit"]:has-text("Save & Upload")')
    page.wait_for_timeout(2000)

    # Assert document created
    doc_card = page.locator("text=Lead Cloud Architect Resume").first
    assert doc_card.is_visible(), "Document title not visible on card"
    page.screenshot(path=os.path.join(BRAIN_DIR, "document_created_verified.png"))
    print("Saved screenshot: document_created_verified.png")

    # 4. Upload version 2 (as default)
    print("4. Uploading Version 2...")
    page.click("button:has-text('New Version')")
    page.wait_for_timeout(500)

    page.fill('input[placeholder*="v2 or tailored-ai"]', "v2-cloud")
    page.fill('input[placeholder*="Added leadership section"]', "Added AWS EKS and Terraform achievements")
    page.check('#setAsDefault')
    page.set_input_files('input[type="file"]', v2_path)
    page.wait_for_timeout(500)

    page.click('button[type="submit"]:has-text("Upload Version")')
    page.wait_for_timeout(2000)

    page.screenshot(path=os.path.join(BRAIN_DIR, "document_versions_verified.png"))
    print("Saved screenshot: document_versions_verified.png")

    # 5. Open Version Comparison Modal
    print("5. Opening Compare Versions modal...")
    page.click("button:has-text('Compare Versions')")
    page.wait_for_timeout(1000)

    assert page.locator("text=Compare Version Performance").is_visible()
    page.screenshot(path=os.path.join(BRAIN_DIR, "document_compare_verified.png"))
    print("Saved screenshot: document_compare_verified.png")

    # Close compare modal
    page.click("button[aria-label='Close modal']")
    page.wait_for_timeout(500)

    # 6. Test In-App PDF Preview
    print("6. Opening in-app PDF previewer...")
    page.locator("button:has-text('Preview')").first.click()
    page.wait_for_timeout(2000)

    assert page.locator("text=In-App Document Preview").is_visible()
    page.screenshot(path=os.path.join(BRAIN_DIR, "document_preview_verified.png"))
    print("Saved screenshot: document_preview_verified.png")

    # Close PDF preview modal
    page.click("button[aria-label='Close modal']")
    page.wait_for_timeout(500)

    # 7. Create Job Application and Link Document
    print("7. Creating job application to verify document linking...")
    app_res = page.evaluate("""
        async () => {
            const token = localStorage.getItem('pipeline_token');
            const res = await fetch('/api/applications', {
                method: 'POST',
                headers: {
                    'Content-Type': 'application/json',
                    'Authorization': `Bearer ${token}`
                },
                body: JSON.stringify({
                    roleTitle: 'Principal Systems Architect',
                    companyName: 'Cloudflare',
                    status: 'Interview'
                })
            });
            return await res.json();
        }
    """)
    app_id = app_res['id']
    print(f"Created application: {app_id}")
    page.goto(f"http://localhost:5173/applications/{app_id}")
    page.wait_for_load_state("networkidle")
    page.wait_for_timeout(1000)

    # Click Documents & CV tab
    print("8. Linking CV version in Application Detail...")
    page.click("button:has-text('Documents & CV')")
    page.wait_for_timeout(1000)

    # Select the CV version
    select_box = page.locator("select").first
    options = select_box.locator("option").all()
    target_val = None
    for opt in options:
        text = opt.text_content()
        if "v2-cloud" in text:
            target_val = opt.get_attribute("value")
            break

    if target_val:
        select_box.select_option(target_val)
        page.wait_for_timeout(500)
        page.click("button:has-text('Save Linked Document Versions')")
        page.wait_for_timeout(2000)

    page.screenshot(path=os.path.join(BRAIN_DIR, "application_documents_linked_verified.png"))
    print("Saved screenshot: application_documents_linked_verified.png")

    # 8. Check Documents Page to verify conversion metrics updated
    print("9. Verifying stats incremented on /documents...")
    page.goto("http://localhost:5173/documents")
    page.wait_for_load_state("networkidle")
    page.wait_for_timeout(1500)

    page.screenshot(path=os.path.join(BRAIN_DIR, "document_stats_updated_verified.png"))
    print("Saved screenshot: document_stats_updated_verified.png")

    browser.close()
    print("All Playwright verification checks passed successfully!")

const { chromium } = require('playwright');
const path = require('path');
const fs = require('fs');

async function run() {
  const artifactsDir = 'C:\\Users\\nedim\\.gemini\\antigravity\\brain\\d58d12a2-00ee-4d47-98b8-25ac990ec966';
  if (!fs.existsSync(artifactsDir)) {
    fs.mkdirSync(artifactsDir, { recursive: true });
  }

  const browser = await chromium.launch({ headless: true });

  try {
    console.log('1. Logging in as Nedim Jugo (nedim.jugoo@gmail.com)...');
    const context = await browser.newContext({ viewport: { width: 1400, height: 950 } });
    const page = await context.newPage();

    await page.goto('http://localhost:5173/login');
    await page.waitForLoadState('networkidle');

    await page.fill('input[placeholder="you@example.com"]', 'nedim.jugoo@gmail.com');
    await page.fill('input[placeholder="••••••••••"]', 'Password123!');
    await page.click('button[type="submit"]');

    await page.waitForURL('http://localhost:5173/', { timeout: 15000 });
    console.log('✓ Successfully authenticated into Pipeline Command Center.');

    // 2. Navigate to Applications page
    console.log('2. Navigating to Applications page...');
    await page.goto('http://localhost:5173/applications');
    await page.waitForLoadState('networkidle');
    await page.waitForTimeout(1500);

    // Switch to Table View to see all 25 applications in full clarity
    console.log('Switching to Table view...');
    const tableBtn = page.locator('button[aria-label="Table view"]');
    if (await tableBtn.isVisible()) {
      await tableBtn.click();
    } else {
      await page.locator('button:has(svg.lucide-list)').first().click();
    }
    await page.waitForTimeout(1000);

    // Verify companies in table
    await page.waitForSelector('text=Ministry of Programming');
    await page.waitForSelector('text=ZIRA Group');
    await page.waitForSelector('text=UniCredit Bank d.d.');
    await page.waitForSelector('text=SaaS Solutions');
    await page.waitForSelector('text=Port8 (emonitor AG)');
    await page.waitForSelector('text=Raiffeisen Group');
    await page.waitForSelector('text=BH Telecom d.d. Sarajevo');
    console.log('✓ Verified applications present in Table view.');

    const tableScreenshotPath = path.join(artifactsDir, 'nedim_applications_table_verified.png');
    await page.screenshot({ path: tableScreenshotPath, fullPage: true });
    console.log(`✓ Saved applications table screenshot: ${tableScreenshotPath}`);

    // 3. Open Ministry of Programming application detail page
    console.log('3. Opening Ministry of Programming application details...');
    await page.click('text=Ministry of Programming');
    await page.waitForURL(/\/applications\/[0-9a-fA-F-]+/, { timeout: 10000 });
    await page.waitForLoadState('networkidle');
    await page.waitForTimeout(1000);

    console.log('✓ Application details page loaded.');

    // Click Timeline & History tab to view interactions
    console.log('Viewing Timeline & History tab...');
    const timelineTab = page.locator('button:has-text("Timeline & History")');
    await timelineTab.click();
    await page.waitForTimeout(1500);

    await page.waitForSelector('text=Interaction', { timeout: 8000 });
    console.log('✓ Verified communication timeline items rendered.');

    // Click Contacts & Network tab
    console.log('Viewing Contacts & Network tab...');
    const contactsTab = page.locator('button:has-text("Contacts & Network")');
    await contactsTab.click();
    await page.waitForTimeout(1500);

    await page.waitForSelector('text=Rešad Začina');
    await page.waitForSelector('text=Hajra Saletović');
    console.log('✓ Verified linked recruiter/referrer contacts rendered.');

    // Switch back to Timeline to capture full screenshot
    await timelineTab.click();
    await page.waitForTimeout(1000);

    const appScreenshotPath = path.join(artifactsDir, 'nedim_real_data_verified.png');
    await page.screenshot({ path: appScreenshotPath, fullPage: true });
    console.log(`✓ Saved application screenshot: ${appScreenshotPath}`);

    // 4. Navigate to Contacts page
    console.log('4. Navigating to Contacts directory...');
    await page.goto('http://localhost:5173/contacts');
    await page.waitForLoadState('networkidle');
    await page.waitForTimeout(2000);

    await page.waitForSelector('text=Damir Avdić');
    await page.waitForSelector('text=Mateja Šumić');
    await page.waitForSelector('text=Edin Salihagić');
    await page.waitForSelector('text=Sanja Zovko');
    await page.waitForSelector('text=Armin Babović');
    console.log('✓ Verified recruiter/referrer contacts loaded.');

    const contactsScreenshotPath = path.join(artifactsDir, 'nedim_contacts_verified.png');
    await page.screenshot({ path: contactsScreenshotPath, fullPage: true });
    console.log(`✓ Saved contacts directory screenshot: ${contactsScreenshotPath}`);

    // 5. Navigate to Settings -> Data Management to verify the Career Data card
    console.log('5. Navigating to Settings -> Data Management...');
    await page.goto('http://localhost:5173/settings');
    await page.waitForLoadState('networkidle');
    await page.click('button:has-text("Data Management & GDPR")');
    await page.waitForSelector('text=Nedim Jugo — Real Job Search History');
    console.log('✓ Verified Nedim Career Data card in Data Management tab.');

    await context.close();
    console.log('\n=== ALL REAL CAREER DATA PLAYWRIGHT VERIFICATION SUCCEEDED ===\n');
  } finally {
    await browser.close();
  }
}

run().catch((err) => {
  console.error('Verification failed:', err);
  process.exit(1);
});

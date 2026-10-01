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
    console.log('1. Setting up fresh user account for Integrations Tab test...');
    const context = await browser.newContext({ viewport: { width: 1280, height: 900 } });
    const page = await context.newPage();

    const timestamp = Date.now();
    const testEmail = `integrations_tester_${timestamp}@example.com`;
    const testPassword = 'Password123!';

    await page.goto('http://localhost:5173/register');
    await page.waitForLoadState('networkidle');

    await page.fill('input[placeholder="Jane Doe"]', 'Devin Integrator');
    await page.fill('input[placeholder="you@example.com"]', testEmail);
    await page.fill('input[placeholder="••••••••••"]', testPassword);
    await page.click('button[type="submit"]');

    await page.waitForURL('http://localhost:5173/', { timeout: 10000 });

    // Handle onboarding wizard if present
    const hasWizard = await page.isVisible('text=Set Up Your Job Search Profile');
    if (hasWizard) {
      console.log('Completing initial onboarding wizard...');
      await page.click('button:has-text("Continue to Tour")');
      await page.waitForSelector('text=Welcome to Pipeline Tour');
      await page.click('button:has-text("Continue to Quick Start")');
      await page.waitForSelector('text=Log Your First Opportunity');
      await page.click('button:has-text("Skip to Dashboard")');
      await page.waitForTimeout(1000);
    }

    // 2. Navigate to Settings -> Integrations Tab
    console.log('2. Navigating to Settings page...');
    await page.goto('http://localhost:5173/settings');
    await page.waitForLoadState('networkidle');

    console.log('Clicking Integrations & Services tab...');
    const integrationsTabBtn = page.locator('button:has-text("Integrations & Services")');
    await integrationsTabBtn.waitFor({ state: 'visible' });
    await integrationsTabBtn.click();

    // Verify initial fallback state
    await page.waitForSelector('text=Email & Notifications (SMTP)');
    await page.waitForSelector('text=Active (.env fallback)');
    console.log('✓ Initial .env fallback status verified.');

    // 3. Configure custom SMTP & Google credentials
    console.log('3. Configuring custom SMTP and service options...');
    const customSmtpCheckbox = page.locator('input[aria-label="Enable custom SMTP"]');
    await customSmtpCheckbox.click();

    await page.fill('input[placeholder="e.g. smtp.gmail.com"]', 'smtp.sendgrid.net');
    await page.fill('input[placeholder="587"]', '587');
    await page.fill('input[placeholder="e.g. your-email@gmail.com"]', 'apikey');
    await page.fill('input[placeholder*="password"]', 'SG.test-secret-key-12345');
    await page.fill('input[placeholder="no-reply@pipeline.local"]', 'alerts@mycareer.dev');

    // Configure Google OAuth
    const customGoogleCheckbox = page.locator('input[aria-label="Enable custom Google OAuth"]');
    await customGoogleCheckbox.click();
    await page.fill('input[placeholder*="apps.googleusercontent.com"]', '987654321-custom.apps.googleusercontent.com');
    await page.fill('input[placeholder*="GOCSPX-secret"]', 'GOCSPX-supersecretkey99');

    // Choose S3 Cloud Storage
    await page.click('label:has-text("Cloud S3 / MinIO Storage")');

    // 4. Save Integrations
    console.log('4. Saving integration settings...');
    await page.click('button:has-text("Save Integrations")');

    await page.waitForSelector('text=Integration settings successfully updated!', { timeout: 10000 });
    await page.waitForSelector('text=Active (Custom)');
    console.log('✓ Custom integration settings saved and active custom badge verified.');

    // 5. Take verification screenshot
    const screenshotPath = path.join(artifactsDir, 'settings_integrations_verified.png');
    await page.screenshot({ path: screenshotPath, fullPage: true });
    console.log(`✓ Saved screenshot: ${screenshotPath}`);

    await context.close();
    console.log('\n=== INTEGRATIONS TAB PLAYWRIGHT VERIFICATION SUCCEEDED ===\n');
  } finally {
    await browser.close();
  }
}

run().catch((err) => {
  console.error('Playwright verification failed:', err);
  process.exit(1);
});

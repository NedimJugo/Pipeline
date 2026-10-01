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
    // -------------------------------------------------------------
    // Part 1: Registration & Onboarding Wizard (Desktop / Tablet)
    // -------------------------------------------------------------
    console.log('1. Testing User Registration & Onboarding Wizard...');
    const context = await browser.newContext({ viewport: { width: 1280, height: 850 } });
    const page = await context.newPage();

    page.on('console', (msg) => console.log('PAGE LOG:', msg.text()));
    page.on('pageerror', (err) => console.log('PAGE ERROR:', err.message));

    const timestamp = Date.now();
    const testEmail = `seeker_${timestamp}@example.com`;
    const testPassword = 'Password123!';

    await page.goto('http://localhost:5173/register');
    await page.waitForLoadState('networkidle');

    await page.fill('input[placeholder="Jane Doe"]', 'Morgan Vance');
    await page.fill('input[placeholder="you@example.com"]', testEmail);
    await page.fill('input[placeholder="••••••••••"]', testPassword);
    await page.click('button[type="submit"]');

    // Wait for redirect to dashboard and onboarding modal to appear
    await page.waitForURL('http://localhost:5173/', { timeout: 10000 });
    await page.waitForSelector('text=Set Up Your Job Search Profile', { timeout: 10000 });
    console.log('✓ Onboarding wizard popped up on first registration.');

    // Step 1: Preferences
    await page.fill('input[placeholder*="Senior Software Engineer"]', 'Principal Distributed Systems Engineer');
    await page.click('button:has-text("Continue to Tour")');

    // Step 2: Tour slides
    await page.waitForSelector('text=Welcome to Pipeline Tour');
    console.log('✓ Step 2 (App Tour) reached.');

    // Take screenshot of Onboarding Wizard (Tour Step)
    const onboardingImgPath = path.join(artifactsDir, 'onboarding_wizard_verified.png');
    await page.screenshot({ path: onboardingImgPath });
    console.log(`✓ Saved screenshot: ${onboardingImgPath}`);

    // Click Continue to Quick Start
    await page.click('button:has-text("Continue to Quick Start")');
    await page.waitForSelector('text=Log Your First Opportunity');

    // Finish / Skip to Dashboard
    await page.click('button:has-text("Skip to Dashboard")');
    await page.waitForTimeout(1500);

    // Verify Onboarding completed: Reload page and assert wizard is gone
    await page.reload();
    await page.waitForLoadState('networkidle');
    const wizardVisible = await page.isVisible('text=Set Up Your Job Search Profile');
    if (wizardVisible) {
      throw new Error('Onboarding wizard appeared again after completion!');
    }
    console.log('✓ Onboarding persistence verified: does not reappear on reload.');

    // -------------------------------------------------------------
    // Part 2: Historical Application Date Logging (Past date allowed, future blocked)
    // -------------------------------------------------------------
    console.log('2. Testing Historical Application Date Logging...');
    await page.goto('http://localhost:5173/applications');
    await page.waitForLoadState('networkidle');

    // Click New Application button
    await page.click('button:has-text("New Application")');
    
    // Wait directly for the date input inside modal
    const dateInput = page.locator('#create-app-date');
    await dateInput.waitFor({ state: 'visible', timeout: 5000 });

    const maxDate = await dateInput.getAttribute('max');
    const todayStr = new Date().toISOString().split('T')[0];
    console.log(`Date picker max attribute: ${maxDate} (today: ${todayStr})`);
    if (maxDate !== todayStr) {
      throw new Error(`Expected max date to be ${todayStr}, got ${maxDate}`);
    }

    // Set a historical backfilled date: 2 months in the past
    const pastDate = '2026-08-15';
    await dateInput.fill(pastDate);

    // Fill application form inside modal
    await page.fill('input[placeholder="e.g. Stripe, OpenAI, Figma"]', 'Vercel Infrastructure');
    await page.fill('input[placeholder="e.g. Senior Software Engineer"]', 'Principal Systems Architect');
    await page.selectOption('#create-app-status', 'Interview');

    // Submit application
    await page.click('button:has-text("Add to Pipeline")');
    
    // Wait for modal to close
    await dateInput.waitFor({ state: 'detached', timeout: 5000 });
    console.log('✓ Modal closed successfully after submit.');

    // Wait for card in Kanban
    await page.waitForSelector('text=Vercel Infrastructure', { timeout: 10000 });
    console.log('✓ Successfully created application with historical date 2026-08-15.');

    // Capture screenshot of Historical Application logged
    const historicalImgPath = path.join(artifactsDir, 'historical_date_application_verified.png');
    await page.screenshot({ path: historicalImgPath });
    console.log(`✓ Saved screenshot: ${historicalImgPath}`);

    await context.close();

    // -------------------------------------------------------------
    // Part 3: Mobile Viewport (375x812 - iPhone SE/Mini) Drawer Navigation
    // -------------------------------------------------------------
    console.log('3. Testing Mobile Responsive Navigation Drawer (375x812)...');
    const mobileContext = await browser.newContext({
      viewport: { width: 375, height: 812 },
      isMobile: true,
      hasTouch: true,
    });
    const mobilePage = await mobileContext.newPage();

    // Login with the created test account
    await mobilePage.goto('http://localhost:5173/login');
    await mobilePage.waitForLoadState('networkidle');
    await mobilePage.fill('input[type="email"]', testEmail);
    await mobilePage.fill('input[type="password"]', testPassword);
    await mobilePage.click('button[type="submit"]');

    await mobilePage.waitForURL('http://localhost:5173/', { timeout: 10000 });
    await mobilePage.waitForLoadState('networkidle');

    // Verify Hamburger button is visible
    const hamburgerBtn = mobilePage.locator('button[aria-label="Toggle navigation menu"]');
    await hamburgerBtn.waitFor({ state: 'visible', timeout: 5000 });
    console.log('✓ Hamburger button visible on 375px mobile viewport.');

    // Click Hamburger to open drawer
    await hamburgerBtn.click();
    await mobilePage.waitForSelector('[data-testid="mobile-drawer-backdrop"]', { timeout: 5000 });
    await mobilePage.waitForTimeout(350);
    console.log('✓ Mobile navigation drawer backdrop opened.');

    // Take screenshot of Mobile Navigation Drawer
    const mobileImgPath = path.join(artifactsDir, 'mobile_navigation_verified.png');
    await mobilePage.screenshot({ path: mobileImgPath });
    console.log(`✓ Saved screenshot: ${mobileImgPath}`);

    // Click Applications in drawer and verify drawer auto-closes and navigates
    const drawerApplicationsLink = mobilePage.locator('aside nav a:has-text("Applications")');
    await drawerApplicationsLink.click();
    await mobilePage.waitForURL('**/applications');
    await mobilePage.waitForSelector('[data-testid="mobile-drawer-backdrop"]', { state: 'detached', timeout: 5000 });
    console.log('✓ Drawer auto-closed upon selecting nav link.');

    await mobileContext.close();

    console.log('\n=== ALL PLAYWRIGHT E2E VERIFICATIONS PASSED ===\n');
  } catch (e) {
    console.error('Test error caught:', e);
    throw e;
  } finally {
    await browser.close();
  }
}

run().catch((err) => {
  console.error('Playwright verification failed:', err);
  process.exit(1);
});

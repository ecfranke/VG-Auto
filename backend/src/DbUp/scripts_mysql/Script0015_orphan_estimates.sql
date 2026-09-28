-- Estimates of deleted work were left behind (the offer was deleted, the estimate was not). A new work that got the
-- deleted work's number then could not issue its offer: "Duplicate entry ... uq_estimate_company_number".
-- Work with a sent offer cannot be deleted, so these are unsent drafts nothing refers to.
CREATE TEMPORARY TABLE orphan_estimate AS
SELECT e.id FROM estimate e WHERE NOT EXISTS (SELECT 1 FROM offer o WHERE o.estimateid = e.id);

DELETE FROM pricingline WHERE pricingid IN (SELECT id FROM orphan_estimate);
DELETE FROM estimate WHERE id IN (SELECT id FROM orphan_estimate);
DELETE FROM pricing WHERE id IN (SELECT id FROM orphan_estimate);
DROP TEMPORARY TABLE orphan_estimate;

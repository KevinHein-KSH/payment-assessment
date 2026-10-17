import type { CustomerCard } from "@/lib/types";

interface PaymentMethodOptionsProps {
  cards: CustomerCard[];
  // null means "Pay with a new payment method"
  selectedCardId: number | null;
  onChange: (cardId: number | null) => void;
}

export default function PaymentMethodOptions({
  cards,
  selectedCardId,
  onChange,
}: PaymentMethodOptionsProps) {
  return (
    <fieldset>
      <legend className="text-sm font-semibold text-gray-900">Payment method</legend>
      <div className="mt-3 space-y-2">
        {cards.map((card) => (
          <PaymentMethodOption
            key={card.id}
            label="Saved card"
            detail={card.maskedCardNo}
            checked={selectedCardId === card.id}
            onSelect={() => onChange(card.id)}
          />
        ))}
        <PaymentMethodOption
          label="Pay with a new payment method"
          checked={selectedCardId === null}
          onSelect={() => onChange(null)}
        />
      </div>
    </fieldset>
  );
}

interface PaymentMethodOptionProps {
  label: string;
  detail?: string;
  checked: boolean;
  onSelect: () => void;
}

function PaymentMethodOption({ label, detail, checked, onSelect }: PaymentMethodOptionProps) {
  return (
    <label
      className={`flex cursor-pointer items-center gap-3 rounded-md border px-4 py-3 text-sm ${
        checked ? "border-blue-600 bg-blue-50" : "border-gray-200 hover:bg-gray-50"
      }`}
    >
      <input
        type="radio"
        name="paymentMethod"
        checked={checked}
        onChange={onSelect}
        className="h-4 w-4 accent-blue-600"
      />
      <span className="text-gray-900">{label}</span>
      {detail && <span className="ml-auto font-mono text-gray-600">{detail}</span>}
    </label>
  );
}

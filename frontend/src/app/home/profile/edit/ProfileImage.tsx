'use client';

import Image from 'next/image';
import { useState } from 'react';
import { IUserProfile } from '../model';


export default function ProfileImage({
    options
}: {
    options: IUserProfile
}) {

    const [selectedImage, setSelectedImage] = useState(encodeURIComponent("data:image/png;base64, " + options.profileImageBase64))
    const [selectedBase64Image,setSelectedBase64Image] = useState(options.profileImageBase64??'');
    const toBase64 = (file: Blob) => new Promise((resolve, reject) => {
        const reader = new FileReader();
        reader.readAsDataURL(file);
        reader.onload = () => resolve(reader.result);
        reader.onerror = reject;
    });
    async function handleImageUpload(event: React.ChangeEvent<HTMLInputElement>): Promise<void> {
        if (event.currentTarget.files && event.currentTarget.files.length > 0) {
            const file = event.currentTarget.files[0];

            const fsize = file.size;
            const fileSize = Math.round((fsize / 1024));
            // The size of the file.
            if (fileSize >= 5120) {
                alert("Image too big, please select a file not bigger than 5mb");
            }
            else {
                const base64File = await toBase64(file) as string; 
                setSelectedBase64Image(base64File.split("base64,")[1]);
                setSelectedImage(encodeURIComponent(base64File));
            }    
        }
    }

    return (
        <div className="col-span-full flex items-center gap-x-8">
            <Image
                alt={`${options.firstName} ${options.lastName}`}
                src={decodeURIComponent(selectedImage)}
                width={100}
                height={100}
                className="size-24 flex-none rounded-lg bg-gray-800 object-cover"
            />
            <div>
                <input type='hidden' name='profileImageBase64' value={selectedBase64Image}></input>
                <input type="file" id='imageUpload' hidden={true} accept="image/*" onChange={handleImageUpload} />
                <button
                    type="button"
                    onClick={() => {
                        document.getElementById('imageUpload')?.click();
                    }}
                    className="rounded-md bg-white px-3 py-2 text-sm font-semibold text-gray-900 ring-1 shadow-xs ring-gray-300 ring-inset hover:bg-gray-50"
                >
                    Change avatar
                </button>
                <p className="mt-2 text-xs/5 text-gray-400">JPG, GIF or PNG. 5MB max.</p>
            </div>
        </div>
    )
}